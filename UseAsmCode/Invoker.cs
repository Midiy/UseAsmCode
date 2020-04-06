using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace UseAsmCode
{
    /// <summary>
    /// Класс, предоставляющий методы для вызова двоичного машинного кода.
    /// </summary>
    public static unsafe class Invoker
    {
        /// <summary>
        /// Смещение адреса возврата относительно верхушки стека при запуске без отладчика Visual Studio.
        /// </summary>
        private const int _RETURN_OFFSET = 6;
        /// <summary>
        /// Дополнительное смещение адреса возврата, добавляемое отладчиком Visual Studio (итоговое смещение _RETURN_OFFSET + _DEBUGGER_OFFSET)
        /// </summary>
        private const int _DEBUGGER_OFFSET = 12;
        /// <summary>
        /// Смещение первого аргумента относительно адреса возврата.
        /// </summary>
        private const int _FIRST_ARG_OFFSET = 4;
        /// <summary>
        /// Смещение второго аргумента относительно адре
        /// </summary>
        private const int _SECOND_ARG_OFFSET = 5;

        /// <summary>
        /// Свойство, определяющее, что программа будет выполняться под отладчиком Visual Studio.
        /// </summary>
        public static bool WithVSDebugger = false;
        /// <summary>
        /// Свойство, определяющее, что метод InvokeAsm() перед передачей управления ассемблерной вставке 
        /// будет пытаться автоматически определить наличие отладчика Visual Studio.
        /// </summary>
        public static bool AutoDetectVSDebugger = true;

        [DllImport("kernel32.dll", EntryPoint = "VirtualProtect")]
        private static extern bool _virtualProtect(int* lpAddress, uint dwSize, uint flNewProtect, uint* lpflOldProtect);

        [DllImport("kernel32.dll", EntryPoint = "IsBadCodePtr")]
        private static extern int _isBadCodePtr(void* ptr);

        /// <summary>
        /// Передаёт управление машинному коду.
        /// </summary>
        /// <param name="firstAsmArg"> Первый аргумент, передаваемый машинному коду. При использовании SASM доступен как $first. </param>
        /// <param name="secondAsmArg"> Второй аргумент, передаваемый машинному коду. При использовании SASM доступен как $second. </param>
        /// <param name="code"> Двоичный код, которому будет передано управление. </param>
        /// <returns> Возвращается значение EAX после выполнения машинного кода. </returns>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void* InvokeAsm(void* firstAsmArg, void* secondAsmArg, byte[] code)
        {
            GCHandle gcLock = GCHandle.Alloc(code, GCHandleType.Pinned);
            void* result = _invokeAsm(firstAsmArg, secondAsmArg, code);
            gcLock.Free();
            return result;
        }
        
        /// <summary>
        /// Передаёт управление машинному коду.
        /// </summary>
        /// <param name="firstAsmArg"> Первый аргумент, передаваемый машинному коду. При использовании SASM доступен как $first. </param>
        /// <param name="secondAsmArg"> Второй аргумент, передаваемый машинному коду. При использовании SASM доступен как $second. </param>
        /// <param name="code"> Двоичный код, которому будет передано управление. </param>
        /// <returns> Возвращается значение EAX после выполнения машинного кода. </returns>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void* _invokeAsm(void* firstAsmArg, void* secondAsmArg, byte[] code)
        {
            int i = 0;
            int* p = &i;
            p += _RETURN_OFFSET;
            if (WithVSDebugger || (AutoDetectVSDebugger && _isBadCodePtr((void*)*p) != 0))
            {
                p += _DEBUGGER_OFFSET;
                *(p - _FIRST_ARG_OFFSET) = (int)firstAsmArg;
                *(p - _SECOND_ARG_OFFSET) = (int)secondAsmArg;
            }
            i = *p;
            fixed (byte* b = code)
            {
                *p = (int)b;
                uint prev;
                _virtualProtect((int*)b, (uint)code.Length, 0x40, &prev);
            }
            return (void*)i;
        }

        /// <summary>
        /// Обёртка над <see cref="InvokeAsm(void*, void*, byte[])"/>, позволяющая вместо указателей передавать объекты
        /// (в т.ч. и управляемые), а также уточнять тип возвращаемого значения.
        /// </summary>
        /// <typeparam name="T1"> Тип первого аргумента машинного кода. </typeparam>
        /// <typeparam name="T2"> Тип второго аргумента машинного кода. </typeparam>
        /// <typeparam name="Tret"> Тип возвращаемого значения. </typeparam>
        /// <param name="firstAsmArg"> Первый аргумент, передаваемый машинному коду. При использовании SASM доступен как $first. </param>
        /// <param name="secondAsmArg"> Второй аргумент, передаваемый машинному коду. При использовании SASM доступен как $second. </param>
        /// <param name="code"> Двоичный код, которому будет передано управление. </param>
        /// <returns> 
        /// Возвращается объект типа <typeparamref name="Tret"/>, на который указывает указатель, возвращаемый
        /// InvokeAsm(<paramref name="firstAsmArg"/>, <paramref name="secondAsmArg"/>, <paramref name="code"/>).
        /// </returns>
        /// <seealso cref="InvokeAsm(void*, void*, byte[])"/>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static Tret SafeInvokeAsm<T1, T2, Tret>(ref T1 firstAsmArg, ref T2 secondAsmArg, byte[] code)
        {
            typeof(GC).InvokeMember("TryStartNoGCRegion",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.InvokeMethod, null, null, new object[] { 0, true });
            void* ptrFirst = ToPointer(ref firstAsmArg);
            void* ptrSecond = ToPointer(ref secondAsmArg);
            void* ptrResult = InvokeAsm(ptrFirst, ptrSecond, code);
            Tret result;
            if (typeof(Tret).IsPrimitive || typeof(Tret).IsPointer)
            {
                result = (Tret)Convert.ChangeType((int)ptrResult, typeof(Tret));
            }
            else
            {
                result = ToInstance<Tret>(ptrResult);
            }
            typeof(GC).InvokeMember("EndNoGCRegion",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.InvokeMethod, null, null, null);
            return result;
        }

        /// <summary>
        /// Обёртка над <see cref="InvokeAsm(void*, void*, byte[])"/>, 
        /// позволяющая вместо указателей передавать объекты (в т.ч. и управляемые). <br/>
        /// Используется для вызова машинного кода, который ничего не возвращает (значение EAX игнорируется).
        /// </summary>
        /// <typeparam name="T1"> Тип первого аргумента машинного кода. </typeparam>
        /// <typeparam name="T2"> Тип второго аргумента машинного кода. </typeparam>
        /// <param name="firstAsmArg"> Первый аргумент, передаваемый машинному коду. При использовании SASM доступен как $first. </param>
        /// <param name="secondAsmArg"> Второй аргумент, передаваемый машинному коду. При использовании SASM доступен как $second. </param>
        /// <param name="code"> Двоичный код, которому будет передано управление. </param>
        /// <seealso cref="InvokeAsm(void*, void*, byte[])"/>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void SafeInvokeAsm<T1, T2>(ref T1 firstAsmArg, ref T2 secondAsmArg, byte[] code)
        {
            typeof(GC).InvokeMember("TryStartNoGCRegion",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.InvokeMethod, null, null, new object[] { 0, true });
            void* ptrFirst = ToPointer(ref firstAsmArg);
            void* ptrSecond = ToPointer(ref secondAsmArg);
            InvokeAsm(ptrFirst, ptrSecond, code);
            typeof(GC).InvokeMember("EndNoGCRegion",
                 System.Reflection.BindingFlags.Public |
                 System.Reflection.BindingFlags.Static |
                 System.Reflection.BindingFlags.InvokeMethod, null, null, null);
        }

        /// <summary>
        /// Обёртка над <see cref="InvokeAsm(void*, void*, byte[])"/>, позволяющая вместо указателей передавать объекты 
        /// (в т.ч. и управляемые), а также уточнять тип возвращаемого значения.
        /// </summary>
        /// <typeparam name="T1"> Тип первого аргумента машинного кода. </typeparam>
        /// <typeparam name="T2"> Тип второго аргумента машинного кода. </typeparam>
        /// <typeparam name="Tret"> Тип возвращаемого значения. </typeparam>
        /// <param name="firstAsmArg"> Первый аргумент, передаваемый машинному коду. При использовании SASM доступен как $first. </param>
        /// <param name="secondAsmArg"> Второй аргумент, передаваемый машинному коду. При использовании SASM доступен как $second. </param>
        /// <param name="code"> Объект, описывающий SASM-код, которому будет передано управление. </param>
        /// <returns> 
        /// Возвращается объект типа <typeparamref name="Tret"/>, на который указывает указатель, возвращаемый
        /// InvokeAsm(<paramref name="firstAsmArg"/>, <paramref name="secondAsmArg"/>, <paramref name="code"/>).
        /// </returns>
        /// <seealso cref="InvokeAsm(void*, void*, byte[])"/>
        /// <seealso cref="SASMCode"/>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static Tret SafeInvokeAsm<T1, T2, Tret>(ref T1 firstAsmArg, ref T2 secondAsmArg, SASMCode code)
        {
            return SafeInvokeAsm<T1, T2, Tret>(ref firstAsmArg, ref secondAsmArg, (byte[])code);
        }

        /// <summary>
        /// Обёртка над <see cref="InvokeAsm(void*, void*, byte[])"/>, 
        /// позволяющая вместо указателей передавать объекты (в т.ч. и управляемые). <br/>
        /// Используется для вызова машинного кода, который ничего не возвращает (значение EAX игнорируется).
        /// </summary>
        /// <typeparam name="T1"> Тип первого аргумента машинного кода. </typeparam>
        /// <typeparam name="T2"> Тип второго аргумента машинного кода. </typeparam>
        /// <param name="firstAsmArg"> Первый аргумент, передаваемый машинному коду. При использовании SASM доступен как $first. </param>
        /// <param name="secondAsmArg"> Второй аргумент, передаваемый машинному коду. При использовании SASM доступен как $second. </param>
        /// <param name="code"> Объект, описывающий SASM-код, которому будет передано управление. </param>
        /// <seealso cref="InvokeAsm(void*, void*, byte[])"/>
        /// <seealso cref="SASMCode"/>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void SafeInvokeAsm<T1, T2>(ref T1 firstAsmArg, ref T2 secondAsmArg, SASMCode code)
        {
            SafeInvokeAsm(ref firstAsmArg, ref secondAsmArg, (byte[])code);
        }

        /// <summary>
        /// Позволяет получить объект заданного типа (в т.ч. управляемый) из указателя на него. <br/>
        /// Не работает с указателями на примитивные типы и на указатели.
        /// </summary>
        /// <typeparam name="Tout"> Тип объекта, находящегося по адресу, заданному указателем. </typeparam>
        /// <param name="ptr"> Указатель на объект типа <typeparamref name="Tout"/>. </param>
        /// <returns> Объект типа <typeparamref name="Tout"/>, находящийся по адресу <paramref name="ptr"/>. </returns>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static Tout ToInstance<Tout>(IntPtr ptr)
        {
            Tout temp = default;
            TypedReference tr = __makeref(temp);
            Marshal.WriteIntPtr(*(IntPtr*)(&tr), ptr);
            Tout instance = __refvalue(tr, Tout);
            return instance;
        }

        /// <summary>
        /// Позволяет получить объект заданного типа (в т.ч. управляемый) из указателя на него. <br/>
        /// Не работает с указателями на примитивные типы и на указатели.
        /// </summary>
        /// <typeparam name="Tout"> Тип объекта, находящегося по адресу, заданному указателем. </typeparam>
        /// <param name="ptr"> Указатель на объект типа <typeparamref name="Tout"/>. </param>
        /// <returns> Объект типа <typeparamref name="Tout"/>, находящийся по адресу <paramref name="ptr"/>. </returns>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static Tout ToInstance<Tout>(void* ptr)
        {
            return ToInstance<Tout>(new IntPtr(ptr));
        }

        /// <summary>
        /// Позволяет получить указатель на заданный объект (в т.ч. управляемый).
        /// </summary>
        /// <typeparam name="T"> Тип объекта, указатель на который требуется получить. </typeparam>
        /// <param name="obj"> Объект, указатель на который требуется получить. </param>
        /// <returns> Указатель на объект <paramref name="obj"/>. </returns>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void* ToPointer<T>(ref T obj)
        {
            TypedReference tr = __makeref(obj);
            if (typeof(T).IsValueType)
            {
                return *(void**)&tr;
            }
            else
            {
                return **(void***)&tr;
            }
        }

        /// <summary>
        /// Позволяет получить указатель на заданный объект (в т.ч. управляемый).
        /// </summary>
        /// <typeparam name="T"> Тип объекта, указатель на который требуется получить. </typeparam>
        /// <param name="obj"> Объект, указатель на который требуется получить. </param>
        /// <returns> Указатель на объект <paramref name="obj"/>. </returns>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static IntPtr ToIntPtr<T>(ref T obj)
        {
            return new IntPtr(ToPointer(ref obj));
        }
    }
}
