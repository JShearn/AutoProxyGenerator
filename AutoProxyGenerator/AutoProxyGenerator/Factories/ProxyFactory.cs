using AutoProxyGenerator.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoProxyGenerator
{
    /// <summary>
    /// Factory to create instances of TInterfaceToProxy at runtime
    /// The instance that is created will implement the interceptors supplied by an IInterceptor service
    /// IMPORTANT: Creating a ProxyFactory can be quite resource heavy. Creating the proxy itself is not. Factories should be re-used.
    /// </summary>
    /// <typeparam name="TInterfaceToProxy">Interface to implement</typeparam>
    public class ProxyFactory<TInterfaceToProxy>
    {
        /// <summary>
        /// Creates a new factory that implements the interceptors described using Intercept attributes in TInterfaceToProxy
        /// </summary>
        public ProxyFactory():this(
            new TypeParsingService(typeof(TInterfaceToProxy)), 
            new InterceptorManagerService())
        {
            
        }

        /// <summary>
        /// Creates a new factory that implements interceptors supplied by interceptorSource
        /// </summary>
        /// <param name="interceptorSource">Supplies interceptors</param>
        /// <param name="interceptorMatcher">Manages how interceptors are executed</param>
        public ProxyFactory(IInterceptorSource interceptorSource, IInterceptorManager interceptorMatcher)
        {
            InterceptorMatcher = interceptorMatcher;
            InterceptorSource = interceptorSource;
            Interceptors = interceptorSource.GetInterceptors();
            RegisterInterceptors();

            // Get the app domain and initialize our own assembly with it
            var appDomain = Thread.GetDomain();
            _typeToProxy = typeof(TInterfaceToProxy);
            var assemblyName = new AssemblyName { Name = _typeToProxy.FullName + "_" + _typeToProxy.GUID + ".dll" };
            
            // All shared types get initiated on construction
            var assemblyBuilder = appDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);//, @"C:\Development\Git\auto-proxy-generator\"); //TODO: Remove
            _moduleBuilder = assemblyBuilder.DefineDynamicModule(_typeToProxy.FullName, assemblyName.FullName, true);

            ProxyType = GetProxyType();
            //assemblyBuilder.Save(assemblyName.FullName); //TODO: Remove
        }
        
        protected readonly Type ProxyType;
        protected readonly IEnumerable<IMethodInterceptor> Interceptors;
        protected readonly IInterceptorManager InterceptorMatcher;
        protected readonly IInterceptorSource InterceptorSource;

        /// <summary>
        /// Creates a new instance of TInterfaceToProxy that calls methods within innerInstance
        /// </summary>
        /// <param name="innerInstance">Object to proxy</param>
        /// <returns></returns>
        public virtual TInterfaceToProxy GenerateProxy(TInterfaceToProxy innerInstance)
        {
            if (!Interceptors.Any())
            {
                // No point in proxying this
                return innerInstance;
            }
            
            var proxy = (TInterfaceToProxy)Activator.CreateInstance(ProxyType, innerInstance, InterceptorMatcher);
            return proxy;
        }

        protected virtual void RegisterInterceptors()
        {
            foreach (var method in typeof(TInterfaceToProxy).GetMethods())
            {
                var validInterceptors =
                    InterceptorSource.FindMatchingInterceptors(typeof(TInterfaceToProxy).GetTypeInfo(), method);
                if (validInterceptors.Any())
                {
                    InterceptorMatcher.RegisterMethodInterceptorChain(method, validInterceptors);
                }
            }
        }
        
        protected virtual Type GetProxyType()
        {
            _invokeInterceptorsMethod = GetInvokeInterceptorsMethod(InterceptorMatcher.GetType(), "InvokeInterceptors");
            _invokeInterceptorsMethodVoid = GetInvokeInterceptorsMethod(InterceptorMatcher.GetType(), "InvokeInterceptorsVoid");
            var typeBuilder = CreateTypeBuilder();

            var instanceField = typeBuilder.DefineField("_instance", typeof(object), FieldAttributes.Public);
            var serviceField = typeBuilder.DefineField("_interceptorService", typeof(IInterceptorManager), FieldAttributes.Public);

            CreateConstructor(typeBuilder, instanceField, serviceField);
            CreateMethods(typeBuilder, instanceField, serviceField, InterceptorMatcher, Interceptors);
            return typeBuilder.CreateType();
        }

        #region ILGeneration - Here be dragons

        private MethodInfo _invokeInterceptorsMethod;
        private MethodInfo _invokeInterceptorsMethodVoid;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly Type _typeToProxy;
        
        private TypeBuilder CreateTypeBuilder()
        {
            var typeBuilder = _moduleBuilder.DefineType("ProxyOf" + _typeToProxy.Name,
                  TypeAttributes.Public | TypeAttributes.Class);
            typeBuilder.AddInterfaceImplementation(_typeToProxy);
            return typeBuilder;
        }

        private MethodInfo GetInvokeInterceptorsMethod(Type interceptorType, string name)
        {
            var method = interceptorType.GetMethod(name, BindingFlags.Public | BindingFlags.Instance,
                Type.DefaultBinder,
                new Type[] { typeof(object[]), typeof(object), typeof(string), typeof(string), typeof(string) },
                null);
            if (method == null)
            {
                throw new MissingMethodException(
                    "Could not find the method:\n "+name+"(object,object,string,string,string)\nIn " +
                    interceptorType.FullName);
            }
            return method;
        }

        private FieldBuilder CreateConstructor(TypeBuilder typeBuilder, FieldBuilder instanceField, FieldBuilder serviceField)
        {

            var constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                new Type[] { typeof(object), typeof(IInterceptorManager) });
            // Get the new Object() constructor
            var defaultConstructor = Type.GetType("System.Object")
                .GetConstructor(new Type[] { });
            ILGenerator ilGenerator = constructorBuilder.GetILGenerator();

            // Call the new Object() constructor
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, defaultConstructor);


            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1); // Load constructor argument "instance" on to the stack
            ilGenerator.Emit(OpCodes.Stfld, instanceField); // this._instance = instance

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_2); // Load constructor argument "service" on to the stack
            ilGenerator.Emit(OpCodes.Stfld, serviceField); // this._service = service

            ilGenerator.Emit(OpCodes.Ret);
            return instanceField;
        }

        private void ImplementMethod(MethodInfo mi, TypeBuilder typeBuilder, FieldBuilder instanceField, FieldBuilder serviceField, IInterceptorManager interceptorMatcher, IEnumerable<IMethodInterceptor> interceptors)
        {
            string methodName = mi.Name;
            string methodToken = mi.MetadataToken.ToString();

            // Build a method with the same name as the proxiedMethod
            var methodBuilder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                mi.ReturnType, // Same return type
                mi.GetParameters().Select(pi => pi.ParameterType).ToArray()); // Same paramaters

            var il = methodBuilder.GetILGenerator();

            if (!interceptors.Any() || !interceptorMatcher.MethodHasInterceptors(methodName, methodToken))
            {
                EmitPassThroughMethodBody(il, instanceField, mi);
            }
            else
            {
                EmitProxiedMethodBody(il, instanceField, serviceField, mi, interceptorMatcher);
            }

            //// If we should be returning an unboxed value then unbox it
            //if (mi.ReturnType.IsValueType && mi.ReturnType != typeof(void))
            //    // int unboxedReturnVal = (int)returnVal;
            //    il.Emit(OpCodes.Unbox_Any, mi.ReturnType);

            //if (mi.ReturnType == typeof(void))
            //    il.Emit(OpCodes.Pop); // return;

            il.Emit(OpCodes.Ret); // return unboxedReturnVal;
        }

        private void EmitPassThroughMethodBody(ILGenerator il, FieldBuilder instanceField, MethodInfo method)
        {
            // returnVal = instanceField.method(arg1,arg2...);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, instanceField);
            for (int i = 0; i < method.GetParameters().Length; i++)
            {
                il.Emit(OpCodes.Ldarg_S, i + 1);
            }
            il.Emit(OpCodes.Call, method);
        }

        private void EmitProxiedMethodBody(ILGenerator il, FieldBuilder instanceField, FieldBuilder serviceField, MethodInfo method, IInterceptorManager interceptorMatcher)
        {
            string methodName = method.Name;
            string methodToken = method.MetadataToken.ToString();

            var argArray = EmitArgArray(il, method);

            var proxyMethod = _invokeInterceptorsMethodVoid;
            if (method.ReturnType != typeof(void))
            {
                proxyMethod = _invokeInterceptorsMethod.MakeGenericMethod(new[] { method.ReturnType });
            }
            
            // object returnVal = this._service.ProxyMethod(argArray,_instance,typeName,methodName,methodToken)
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, serviceField);
            il.Emit(OpCodes.Ldloc, argArray);
            il.Emit(OpCodes.Ldarg_0); // Important: it's this._instance not just _instance
            il.Emit(OpCodes.Ldfld, instanceField);
            il.Emit(OpCodes.Ldstr, _typeToProxy.AssemblyQualifiedName ?? _typeToProxy.FullName);
            il.Emit(OpCodes.Ldstr, methodName);
            il.Emit(OpCodes.Ldstr, methodToken);
            il.Emit(OpCodes.Call, proxyMethod);
        }

        private LocalBuilder EmitArgArray(ILGenerator il, MethodInfo method)
        {
            var paramaters = method.GetParameters();

            // object[] argArray
            LocalBuilder argArray = il.DeclareLocal(typeof(object[]));

            // argArray = new object[parameterCount];
            il.Emit(OpCodes.Ldc_I4, paramaters.Length);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc, argArray);

            for (int i = 0; i < paramaters.Length; i++)
            {
                ParameterInfo info = paramaters[i];

                // var arg = firstArgument
                il.Emit(OpCodes.Ldloc, argArray);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldarg_S, i + 1);

                // If arg is a value type then box it so it becomes an object type
                // object boxedArg = arg;
                if (info.ParameterType.IsPrimitive || info.ParameterType.IsValueType)
                    il.Emit(OpCodes.Box, info.ParameterType);

                // argArray.push(boxedArg)
                il.Emit(OpCodes.Stelem_Ref);
            }
            return argArray;
        }

        protected virtual void CreateMethods(TypeBuilder typeBuilder, FieldBuilder instanceField, FieldBuilder serviceField, IInterceptorManager interceptorMatcher, IEnumerable<IMethodInterceptor> interceptors)
        {
            // Find all of the methods that are described in T and implement them
            // (we don't support properties, events etc)
            //TODO: Or do we? They are just get and set accessor methods - test
            foreach (var mi in _typeToProxy.GetMethods())
            {
                ImplementMethod(mi, typeBuilder, instanceField, serviceField, interceptorMatcher, interceptors);
            }
        }


        #endregion
    }
}
