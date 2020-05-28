using System;
using System.Runtime.InteropServices;
using System.Text;
using AOT;

// Default Marshaling for Strings
// https://docs.microsoft.com/en-us/dotnet/framework/interop/default-marshaling-for-strings

namespace jsb
{
    using JSValueConst = JSValue;
    using JSRuntime = IntPtr;
    using JSContext = IntPtr;
    using JS_BOOL = Int32;
    using JSClassID = UInt32;
    using JSAtom = UInt32;

    using size_t = UIntPtr;
    using uint32_t = UInt32;
    using int64_t = Int64;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    public delegate void JSClassFinalizer(JSRuntime rt, JSValue val);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    public delegate JSValue JSCFunction(JSContext ctx, JSValueConst this_val, int argc, ref JSValueConst argv);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    public delegate JSValue JSCFunctionMagic(JSContext ctx, JSValueConst this_val, int argc, ref JSValueConst argv, int magic);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] 
    public delegate JSValue JSCFunctionData(JSContext ctx, JSValueConst this_val, int argc, ref JSValueConst argv, int magic, ref JSValue func_data);
#else
    public delegate void JSClassFinalizer(JSRuntime rt, JSValue val);

    #region TEMP CODE
    public delegate JSValue JSCFunction(JSContext ctx, JSValueConst this_val, int argc, ref JSValueConst argv);
    public delegate JSValue JSCFunctionMagic(JSContext ctx, JSValueConst this_val, int argc, ref JSValueConst argv, int magic);
    public delegate JSValue JSCFunctionData(JSContext ctx, JSValueConst this_val, int argc, ref JSValueConst argv, int magic, ref JSValue func_data);
    #endregion

#endif

    [StructLayout(LayoutKind.Explicit)]
    public struct JSValueUnion
    {
        [FieldOffset(0)]
        public int int32;

        [FieldOffset(0)]
        public double float64;

        [FieldOffset(0)]
        public IntPtr ptr;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JSClassDef
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string class_name; // ok?

        public JSClassFinalizer finalizer;

        public IntPtr gc_mark;
        public IntPtr call;
        public IntPtr exotic;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JSValue
    {
        public JSValueUnion u; // IntPtr
        public long tag;
    }

    public enum JSCFunctionEnum
    {  /* XXX: should rename for namespace isolation */
        JS_CFUNC_generic,
        JS_CFUNC_generic_magic,
        JS_CFUNC_constructor,
        JS_CFUNC_constructor_magic,
        JS_CFUNC_constructor_or_func,
        JS_CFUNC_constructor_or_func_magic,
        JS_CFUNC_f_f,
        JS_CFUNC_f_f_f,
        JS_CFUNC_getter,
        JS_CFUNC_setter,
        JS_CFUNC_getter_magic,
        JS_CFUNC_setter_magic,
        JS_CFUNC_iterator_next,
    };

    [Flags]
    public enum JSPropFlags : uint
    {
        /* flags for object properties */
        JS_PROP_CONFIGURABLE = (1 << 0),
        JS_PROP_WRITABLE = (1 << 1),
        JS_PROP_ENUMERABLE = (1 << 2),
        JS_PROP_C_W_E = (JS_PROP_CONFIGURABLE | JS_PROP_WRITABLE | JS_PROP_ENUMERABLE),
        JS_PROP_LENGTH = (1 << 3) /* used internally in Arrays */,
        JS_PROP_TMASK = (3 << 4) /* mask for NORMAL, GETSET, VARREF, AUTOINIT */,
        JS_PROP_NORMAL = (0 << 4),
        JS_PROP_GETSET = (1 << 4),
        JS_PROP_VARREF = (2 << 4) /* used internally */,
        JS_PROP_AUTOINIT = (3 << 4) /* used internally */,

        /* flags for JS_DefineProperty */
        JS_PROP_HAS_SHIFT = 8,
        JS_PROP_HAS_CONFIGURABLE = (1 << 8),
        JS_PROP_HAS_WRITABLE = (1 << 9),
        JS_PROP_HAS_ENUMERABLE = (1 << 10),
        JS_PROP_HAS_GET = (1 << 11),
        JS_PROP_HAS_SET = (1 << 12),
        JS_PROP_HAS_VALUE = (1 << 13),

        /* throw an exception if false would be returned
           (JS_DefineProperty/JS_SetProperty) */
        JS_PROP_THROW = (1 << 14),
        /* throw an exception if false would be returned in strict mode
           (JS_SetProperty) */
        JS_PROP_THROW_STRICT = (1 << 15),

        JS_PROP_NO_ADD = (1 << 16) /* internal use */,
        JS_PROP_NO_EXOTIC = (1 << 17) /* internal use */,
    }

    public partial class JSBridgeDLL
    {
#if UNITY_IPHONE && !UNITY_EDITOR
	    const string JSBDLL = "__Internal";
#else
        const string JSBDLL = "libquickjs";
#endif

        public const int JS_TAG_FIRST = -11; /* first negative tag */
        public const int JS_TAG_BIG_DECIMAL = -11;
        public const int JS_TAG_BIG_INT = -10;
        public const int JS_TAG_BIG_FLOAT = -9;
        public const int JS_TAG_SYMBOL = -8;
        public const int JS_TAG_STRING = -7;
        public const int JS_TAG_MODULE = -3; /* used internally */
        public const int JS_TAG_FUNCTION_BYTECODE = -2; /* used internally */
        public const int JS_TAG_OBJECT = -1;
        public const int JS_TAG_INT = 0;
        public const int JS_TAG_BOOL = 1;
        public const int JS_TAG_NULL = 2;
        public const int JS_TAG_UNDEFINED = 3;
        public const int JS_TAG_UNINITIALIZED = 4;
        public const int JS_TAG_CATCH_OFFSET = 5;
        public const int JS_TAG_EXCEPTION = 6;
        public const int JS_TAG_FLOAT64 = 7;

        public static JSValue JS_MKVAL(long tag, int val)
        {
            return new JSValue() { u = new JSValueUnion() { int32 = val }, tag = tag };
        }

        public static readonly JSValue JS_NULL = JS_MKVAL(JS_TAG_NULL, 0);
        public static readonly JSValue JS_UNDEFINED = JS_MKVAL(JS_TAG_UNDEFINED, 0);
        public static readonly JSValue JS_FALSE = JS_MKVAL(JS_TAG_BOOL, 0);
        public static readonly JSValue JS_TRUE = JS_MKVAL(JS_TAG_BOOL, 1);
        public static readonly JSValue JS_EXCEPTION = JS_MKVAL(JS_TAG_EXCEPTION, 0);
        public static readonly JSValue JS_UNINITIALIZED = JS_MKVAL(JS_TAG_UNINITIALIZED, 0);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr JS_NewRuntime();

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JS_FreeRuntime(IntPtr rt);


        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr JS_NewContext(IntPtr rt);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JS_FreeContext(IntPtr rt);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr JS_GetContextOpaque(JSContext ctx);
        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JS_SetContextOpaque(JSContext ctx, IntPtr opaque);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern JSValue JS_NewCFunction2(JSContext ctx, IntPtr func,
                                                    [MarshalAs(UnmanagedType.LPStr)] string name,
                                                    int length, JSCFunctionEnum cproto, int magic);

        public static JSValue JS_NewCFunction2(JSContext ctx, JSCFunction func, string name, int length, JSCFunctionEnum cproto, int magic)
        {
            var fn = Marshal.GetFunctionPointerForDelegate(func);
            return JS_NewCFunction2(ctx, fn, name, length, cproto, magic);
        }

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern JSClassID JS_NewClassID(ref JSClassID pclass_id);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JS_NewClass(JSRuntime rt, JSClassID class_id, ref JSClassDef class_def);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JS_IsRegisteredClass(JSRuntime rt, JSClassID class_id);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JS_SetClassProto(JSContext ctx, JSClassID class_id, JSValue obj);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern JSValue JS_GetClassProto(JSContext ctx, JSClassID class_id);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JS_SetConstructor(JSContext ctx, JSValueConst func_obj, JSValueConst proto);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JS_SetPropertyInternal(JSContext ctx, JSValueConst this_obj, JSAtom prop, JSValue val, int flags);

        public static int JS_SetProperty(JSContext ctx, JSValueConst this_obj, JSAtom prop, JSValue val)
        {
            return JS_SetPropertyInternal(ctx, this_obj, prop, val, (int) JSPropFlags.JS_PROP_THROW);
        }

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JS_SetPropertyUint32(JSContext ctx, JSValueConst this_obj, uint32_t idx, JSValue val);
        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JS_SetPropertyInt64(JSContext ctx, JSValueConst this_obj, int64_t idx, JSValue val);
        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JS_SetPropertyStr(JSContext ctx, JSValueConst this_obj, [MarshalAs(UnmanagedType.LPStr)] string prop, JSValue val);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JS_HasProperty(JSContext ctx, JSValueConst this_obj, JSAtom prop);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern JSValue JS_Call(JSContext ctx, JSValueConst func_obj, JSValueConst this_obj,
                       int argc, [MarshalAs(UnmanagedType.LPArray)] JSValueConst[] argv);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern JSValue JS_Invoke(JSContext ctx, JSValueConst this_val, JSAtom atom,
                          int argc, [MarshalAs(UnmanagedType.LPArray)] JSValueConst[] argv);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JS_IsExtensible(JSContext ctx, JSValueConst obj);
        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JS_PreventExtensions(JSContext ctx, JSValueConst obj);
        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JS_DeleteProperty(JSContext ctx, JSValueConst obj, JSAtom prop, int flags);
        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JS_SetPrototype(JSContext ctx, JSValueConst obj, JSValueConst proto_val);
        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern JSValueConst JS_GetPrototype(JSContext ctx, JSValueConst val);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void JS_RunGC(JSRuntime rt);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern JS_BOOL JS_IsLiveObject(JSRuntime rt, JSValueConst obj);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JS_ToInt32(IntPtr ctx, out int pres, JSValue val);

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern JSValue JS_Eval(IntPtr ctx, byte[] input, size_t input_len, byte[] filename, int eval_flags);

        public static JSValue JS_Eval(IntPtr ctx, string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var buf = new byte[bytes.Length + 1];
            bytes.CopyTo(buf, 0);

            var xx = Encoding.UTF8.GetBytes("main");
            var nn = new byte[xx.Length + 1];
            xx.CopyTo(nn, 0);

            return JS_Eval(ctx, buf, (size_t)bytes.Length, nn, 0);
        }

        [DllImport(JSBDLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int JSB_Test(IntPtr ctx);

        // public static bool JS_IsNumber(JSValueConst v)
        // {
        //     var tag = v.tag;
        //     return tag == JS_TAG_INT || JS_TAG_IS_FLOAT64(tag);
        // }

        public static bool JS_IsBigInt(JSContext ctx, JSValueConst v)
        {
            var tag = v.tag;
            return tag == JS_TAG_BIG_INT;
        }

        public static bool JS_IsBigFloat(JSValueConst v)
        {
            var tag = v.tag;
            return tag == JS_TAG_BIG_FLOAT;
        }

        public static bool JS_IsBigDecimal(JSValueConst v)
        {
            var tag = v.tag;
            return tag == JS_TAG_BIG_DECIMAL;
        }

        public static bool JS_IsBool(JSValueConst v)
        {
            return v.tag == JS_TAG_BOOL;
        }

        public static bool JS_IsNull(JSValueConst v)
        {
            return v.tag == JS_TAG_NULL;
        }

        public static bool JS_IsUndefined(JSValueConst v)
        {
            return v.tag == JS_TAG_UNDEFINED;
        }

        public static bool JS_IsException(JSValueConst v)
        {
            return (v.tag == JS_TAG_EXCEPTION);
        }

        public static bool JS_IsUninitialized(JSValueConst v)
        {
            return (v.tag == JS_TAG_UNINITIALIZED);
        }

        public static bool JS_IsString(JSValueConst v)
        {
            return v.tag == JS_TAG_STRING;
        }

        public static bool JS_IsSymbol(JSValueConst v)
        {
            return v.tag == JS_TAG_SYMBOL;
        }

        public static bool JS_IsObject(JSValueConst v)
        {
            return v.tag == JS_TAG_OBJECT;
        }

    }
}
