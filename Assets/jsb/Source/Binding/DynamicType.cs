using System;
using System.Reflection;
using System.Collections.Generic;
using QuickJS.Native;

namespace QuickJS.Binding
{
    public class DynamicType
    {
        public const BindingFlags BaseFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static;
        public const BindingFlags PublicFlags = BaseFlags | BindingFlags.Public;
        public const BindingFlags PrivateFlags = BaseFlags | BindingFlags.NonPublic;
        public const BindingFlags DefaultFlags = BaseFlags | BindingFlags.Public | BindingFlags.NonPublic;

        private Type _type;
        private int _type_id;
        private bool _privateAccess;

        public int id { get { return _type_id; } }

        public Type type { get { return _type; } }

        public bool privateAccess
        {
            get { return _privateAccess; }
        }

        public DynamicType(Type type, bool privateAccess)
        {
            _type = type;
            _type_id = -1;
            _privateAccess = privateAccess;
        }

        public void OpenPrivateAccess()
        {
            if (!_privateAccess)
            {
                _privateAccess = true;
            }
        }

        public bool CheckThis(object self)
        {
            if (self == null)
            {
                return false;
            }
            var type = self.GetType();
            return type == _type || type.IsSubclassOf(_type);
        }

        private void AddMethods(ref ClassDecl cls, bool bStatic, Dictionary<string, List<MethodInfo>> map)
        {
            foreach (var kv in map)
            {
                var methodInfos = kv.Value;
                var methodName = kv.Key;
                var count = methodInfos.Count;
                var dynamicMethod = default(IDynamicMethod);
                if (count == 1)
                {
                    dynamicMethod = new DynamicMethod(this, methodInfos[0]);
                }
                else
                {
                    var overloads = new DynamicMethods(count);
                    for (var i = 0; i < count; i++)
                    {
                        var methodInfo = methodInfos[i];
                        DynamicMethodBase overload;
                        overload = new DynamicMethod(this, methodInfos[i]);
                        overloads.Add(overload);
                    }
                    dynamicMethod = overloads;
                }
                cls.AddMethod(bStatic, methodName, dynamicMethod);
            }
        }

        private void CollectMethod(ref ClassDecl cls, MethodInfo[] methodInfos, Dictionary<string, List<MethodInfo>> instMap, Dictionary<string, List<MethodInfo>> staticMap)
        {
            for (int i = 0, count = methodInfos.Length; i < count; i++)
            {
                var methodInfo = methodInfos[i];
                var name = methodInfo.Name;

                if (methodInfo.IsSpecialName)
                {
                    //TODO: 反射方式的运算符重载注册
                    if (name.StartsWith("op_"))
                    {
                        switch (name)
                        {
                            case "op_Addition":
                            case "op_Subtraction":
                            case "op_Equality":
                            case "op_Multiply":
                            case "op_Division":
                            case "op_UnaryNegation":
                                //TODO: add operators
                                // var op = new DynamicMethod(this, methodInfo);
                                // cls.AddSelfOperator()
                                // cls.AddLeftOperator()
                                // cls.AddRightOperator()
                                break;
                        }
                    }
                    continue;
                }

                var map = methodInfo.IsStatic ? staticMap : instMap;
                List<MethodInfo> list;
                if (!map.TryGetValue(name, out list))
                {
                    list = map[name] = new List<MethodInfo>();
                }
                list.Add(methodInfo);
            }
        }

        public void Bind(TypeRegister register)
        {
            ClassDecl cls;
            var db = register.GetTypeDB();
            var ctx = (JSContext)register.GetContext();
            var flags = DefaultFlags;
            var proto = db.FindPrototypeOf(_type, out _type_id);

            if (proto.IsNullish())
            {
                _type_id = db.AddType(_type, JSApi.JS_UNDEFINED);

                #region BindConstructors(register, flags, type_id);
                var constructors = _type.GetConstructors(flags);
                var dynamicConstructor = default(IDynamicMethod);
                if (constructors.Length > 0)
                {
                    var count = constructors.Length;
                    if (count == 1)
                    {
                        dynamicConstructor = new DynamicConstructor(this, constructors[0]);
                    }
                    else
                    {
                        var overloads = new DynamicMethods(count);
                        for (var i = 0; i < count; i++)
                        {
                            var overload = new DynamicConstructor(this, constructors[i]);
                            overloads.Add(overload);
                        }
                        dynamicConstructor = overloads;
                    }
                }
                #endregion

                cls = register.CreateClass(_type.Name, _type, dynamicConstructor);
            }
            else
            {
                cls = register.CreateClass(_type, proto);
            }

            #region BindMethods(register, flags);
            var instMap = new Dictionary<string, List<MethodInfo>>();
            var staticMap = new Dictionary<string, List<MethodInfo>>();
            CollectMethod(ref cls, _type.GetMethods(flags), instMap, staticMap);
            AddMethods(ref cls, true, staticMap);
            AddMethods(ref cls, false, instMap);
            #endregion

            #region BindFields(register, flags);
            var fieldInfos = _type.GetFields(flags);
            for (int i = 0, count = fieldInfos.Length; i < count; i++)
            {
                var fieldInfo = fieldInfos[i];
                if (!fieldInfo.Name.StartsWith("_JSFIX_")) // skip hotfix slots
                {
                    var dynamicField = new DynamicField(this, fieldInfo);
                    cls.AddField(fieldInfo.IsStatic, fieldInfo.Name, dynamicField);
                }
            }
            #endregion

            #region BindProperties(register, flags);
            var propertyInfos = _type.GetProperties(flags);
            for (int i = 0, count = propertyInfos.Length; i < count; i++)
            {
                var propertyInfo = propertyInfos[i];
                var anyMethod = propertyInfo.GetMethod ?? propertyInfo.SetMethod;
                var dynamicProperty = new DynamicProperty(this, propertyInfo);
                cls.AddField(anyMethod.IsStatic, propertyInfo.Name, dynamicProperty);
            }
            #endregion

            // var ns = new NamespaceDecl();
            // typeDB.AddType()
            cls.Close();
        }
    }
}
