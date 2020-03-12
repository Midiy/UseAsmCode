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
        [DllImport("kernel32.dll")]
        private static extern bool VirtualProtect(int* lpAddress, uint dwSize, uint flNewProtect, uint* lpflOldProtect);
        
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
            p += 0x14 / 4 + 1;
            i = *p;
            fixed (byte* b = code)
            {
                *p = (int)b;
                uint prev;
                VirtualProtect((int*)b, (uint)code.Length, 0x40, &prev);
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

    /// <summary>
    /// Класс, позволяющий транслировать команды ассемблера в двоичный машинный код,
    /// а также получать значения переменных, объявленных в нём.
    /// </summary>
    public class SASMCode
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string FileName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr Module, string ProcName);

        /// <summary>
        /// Класс, описывающий исключения, возникающие при трансляции ассемблерного кода в его двоичное представление.
        /// </summary>
        public class SASMTranslationException : Exception
        {
            /// <summary>
            /// Объект, попытка трансляции которого вызвала исключение. <br/>
            /// Как правило, представляет из себя одну ассемблерную инструкцию.
            /// </summary>
            public object Reason;

            /// <summary>
            /// Конструктор, используемый, если объект, вызвавший исключение, на данный момент неизвестен.
            /// Объект может быть задан позже.
            /// </summary>
            /// <param name="Message"> Сообщение, описывающее причину исключения. </param>
            /// <seealso cref="SASMTranslationException(string, object)"/>
            public SASMTranslationException(string Message) : base(Message)
            {
                Reason = null;
            }

            /// <summary>
            /// Конструктор, используемый, если объект, вызвавший исключение, известен.
            /// </summary>
            /// <param name="Message"> Сообщение, описывающее причину исключения. </param>
            /// <param name="Reason"> Объект, попытка трансляции которого вызвала исключение. </param>
            /// <seealso cref="SASMTranslationException(string)"/>
            public SASMTranslationException(string Message, object Reason) : base(Message)
            {
                this.Reason = Reason;
            }

            /// <summary>
            /// Возвращает строковое представление исключения.
            /// </summary>
            /// <returns> Возвращает конкатенацию сообщения об ошибке и строкового представления объекта <see cref="Reason"/>. </returns>
            public override string ToString()
            {
                return base.Message + Environment.NewLine + Reason.ToString();
            }
        }

        /// <summary>
        /// Перечисление, задающее двоичный код префиксов, которые может иметь инструкция.
        /// </summary>
        private enum Prefixes : byte
        {
            REPE = 0xF3,
            REPNE = 0xF2,
            AddrSize = 0x67,
            OpSize = 0x66,
            //CS = 0x2E,
            //SS = 0x36,
            //DS = 0x3E,
            //ES = 0x26,
            //FS = 0x64,
            //GS = 0x65,
            None = 0
        }

        /// <summary>
        /// Перечисление, задающее двоичный код 32-, 16- и 8-битных регистров процессора.
        /// </summary>
        private enum Registers : byte
        {
            eax = 0b000,
            ebx = 0b011,
            ecx = 0b001,
            edx = 0b010,
            edi = 0b111,
            esi = 0b110,
            esp = 0b100,
            ebp = 0b101,
            ax = 0b000,
            bx = 0b011,
            cx = 0b001,
            dx = 0b010,
            di = 0b111,
            si = 0b110,
            sp = 0b100,
            bp = 0b101,

            al = 0b000,
            bl = 0b011,
            cl = 0b001,
            dl = 0b010,
            ah = 0b100,
            bh = 0b111,
            ch = 0b101,
            dh = 0b110
        }

        /// <summary>
        /// Перечисление, задающее двоичный код различных условных переходов.
        /// </summary>
        private enum Jumps : byte
        {
            jb = 0x82,
            jc = 0x82,
            jnae = 0x82,
            jnb = 0x83,
            jae = 0x83,
            jnc = 0x83,
            ja = 0x87,
            jnbe = 0x87,
            jna = 0x86,
            jbe = 0x86,
            je = 0x84,
            jz = 0x84,
            jne = 0x85,
            jnz = 0x85,
            jl = 0x8C,
            jnge = 0x8C,
            jnl = 0x8D,
            jge = 0x8D,
            jg = 0x8F,
            jnle = 0x8F,
            jng = 0x8E,
            jle = 0x8E,
            jo = 0x80,
            jno = 0x81,
            jp = 0x8A,
            jpe = 0x8A,
            jnp = 0x8B,
            jpo = 0x8B,
            js = 0x88,
            jns = 0x89,
        }

        /// <summary>
        /// Класс, описывающий операнд инструкции.
        /// </summary>
        private class Operand
        {
            /// <summary>
            /// Перечисление, описывающее возможные типы операндов.
            /// </summary>
            public enum OpType : byte
            {
                Const = 0b00000000,
                Register8 = 0b00000001,
                Register16_32 = 0b00000010,
                Register = Register8 | Register16_32,
                Address8 = 0b00000100,
                Address16_32 = 0b00001000,
                Address = Address8 | Address16_32,
                Symbolic = 0b10000000 | Const
            }

            /// <summary>
            /// Тип операнда.
            /// </summary>
            public OpType OperandType;
            /// <summary>
            /// Базовый регистр, если операнд является адресом (<see cref="OperandType"/> = <see cref="OpType.Address"/>).
            /// </summary>
            /// <remarks> 
            /// Формат адресного операнда: 
            /// [&lt;Base&gt; + $lt;Index&gt;*&lt;Scale&gt; + &lt;Offset&gt;]. 
            /// </remarks>
            public Registers? Base;
            /// <summary>
            /// Масштабный индексный регистр, если операнд является адресом (<see cref="OperandType"/> = <see cref="OpType.Address"/>).
            /// </summary>
            /// <remarks> 
            /// Формат адресного операнда: 
            /// [&lt;Base&gt; + $lt;Index&gt;*&lt;Scale&gt; + &lt;Offset&gt;]. 
            /// </remarks>
            public Registers? Index;
            /// <summary>
            /// <para> Масштабный множитель, если операнд является адресом (<see cref="OperandType"/> = <see cref="OpType.Address"/>). </para>
            /// <list type="bullet">
            /// <listheader> Соответствие значений <see cref="Scale"/> и масштабных множителей: </listheader><br/>
            /// <item>
            /// <term> <see cref="Scale"/> = 0b00 </term> :
            /// <description> *1 </description>
            /// </item><br/>
            /// <item>
            /// <term> <see cref="Scale"/> = 0b01 </term> :
            /// <description> *2 </description>
            /// </item><br/>
            /// <item>
            /// <term> <see cref="Scale"/> = 0b10 </term> :
            /// <description> *4 </description>
            /// </item><br/>
            /// <item>
            /// <term> <see cref="Scale"/> = 0b11 </term> :
            /// <description> *8 </description>
            /// </item><br/>
            /// <item>
            /// <term> <see cref="Scale"/> = 255 </term> :
            /// <description> Масштабный множитель отсутствует. </description>
            /// </item><br/>
            /// </list>
            /// </summary>
            /// <remarks> 
            /// Формат адресного операнда: 
            /// [&lt;Base&gt; + $lt;Index&gt;*&lt;Scale&gt; + &lt;Offset&gt;]. 
            /// </remarks>
            public byte Scale;
            /// <summary>
            /// Числовое смещение, если операнд является адресом (<see cref="OperandType"/> = <see cref="OpType.Address"/>).
            /// </summary>
            /// <remarks> 
            /// Формат адресного операнда: 
            /// [&lt;Base&gt; + $lt;Index&gt;*&lt;Scale&gt; + &lt;Offset&gt;]. 
            /// </remarks>
            public int Offset;
            /// <summary>
            /// Значение, если операнд является числовой константой (<see cref="OperandType"/> = <see cref="OpType.Const"/>).
            /// </summary>
            public int Value;
            /// <summary>
            /// Регистр, если операнд является регистром (<see cref="OperandType"/> = <see cref="OpType.Register"/>).
            /// </summary>
            public Registers? Register;
            /// <summary>
            /// Строковое представление операнда.
            /// </summary>
            public string StringOperand;

            /// <summary>
            /// Объект, предоставляющий список доступных (<see cref="_consts"/>) и недоступных (<see cref="_removedConsts"/>) констант.
            /// </summary>
            private SASMCode _codeObj;
            
            /// <summary>
            /// Конструктор, создающий описание операнда путём парсинга его строкового представления.
            /// </summary>
            /// <param name="CodeObj"> Объект типа <see cref="SASMCode"/>, предоставляющий список доступных 
            /// (<see cref="_consts"/>) и недоступных (<see cref="_removedConsts"/>) констант. </param>
            /// <param name="strOperand"> Строковое представление операнда. </param>
            public Operand(SASMCode CodeObj, string strOperand)
            {
                _codeObj = CodeObj;
                bool sizeFlag = false;
                if (strOperand.StartsWith("byte"))
                {
                    sizeFlag = true;
                    strOperand = strOperand.Substring(4);
                }
                else if (strOperand.StartsWith("word"))
                {
                    strOperand = strOperand.Substring(4);
                }
                strOperand = strOperand.Replace(" ", "");
                foreach (string s in CodeObj._removedConsts)
                {
                    if (strOperand.Contains(s)) { throw new SASMTranslationException($"Constant {s} is unaccessible here!"); }
                }
                foreach (string s in CodeObj._consts.Keys)
                {
                    if (strOperand.Contains(s)) { strOperand = strOperand.Replace(s, CodeObj._consts[s]); }
                }
                if (strOperand.Contains("[") && strOperand.Contains("]"))
                {
                    strOperand = strOperand.Replace("[", "");
                    strOperand = strOperand.Replace("]", "");
                    strOperand = "[" + strOperand + "]";
                }
                StringOperand = strOperand;
                if (!_createRegisterOperand())
                {
                    if (!_createConstantOperand())
                    {
                        _createAddressOperand(sizeFlag);
                    }
                }
            }

            /// <summary>
            /// Возвращает строковое представление операнда.
            /// </summary>
            /// <returns> Строковое представление операнда. </returns>
            public override string ToString()
            {
                return StringOperand;
            }

            /// <summary>
            /// Метод, проверяющий, является ли создаваемый операнд регистровым, и если да - создающий его.
            /// </summary>
            /// <returns> Булево значение, показывающее, был ли создан регистровый операнд. </returns>
            private bool _createRegisterOperand()
            {
                HashSet<string> registers = new HashSet<string>
                { "eax", "ebx", "ecx", "edx", "esi", "edi", "ebp", "esp", "ax", "bx", "cx", "dx", "si", "di", "bp", "sp" };
                HashSet<string> registers8 = new HashSet<string> { "ah", "bh", "ch", "dh", "al", "bl", "cl", "dl" };
                if (registers.Contains(StringOperand))
                {
                    OperandType = OpType.Register16_32;
                    Register = (Registers)Enum.Parse(typeof(Registers), StringOperand);
                    Base = Index = null;
                    Scale = 255;
                    Offset = 0;
                    Value = 0;
                    return true;
                }
                else if (registers8.Contains(StringOperand))
                {
                    OperandType = OpType.Register8;
                    Register = (Registers)Enum.Parse(typeof(Registers), StringOperand);
                    Base = Index = null;
                    Scale = 255;
                    Offset = 0;
                    Value = 0;
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Метод, проверяющий, является ли создаваемый операнд константным, и если да - создающий его.
            /// </summary>
            /// <returns> Булево значение, показывающее, был ли создан константный операнд. </returns>
            private bool _createConstantOperand()
            {
                if (_isConstant(StringOperand))
                {
                    OperandType = OpType.Const;
                    Value = _sumAllNums(StringOperand).GetInt();
                    Register = Base = Index = 0;
                    Scale = 255;
                    Offset = 0;
                    return true;
                }
                else if (!StringOperand.Contains("["))
                {
                    if (_codeObj._externs.ContainsKey(StringOperand))
                    {
                        OperandType = OpType.Const;
                        Base = Index = Register = null;
                        Scale = 0;
                        Offset = 0;
                        Value = _codeObj._externs[StringOperand].ToInt32();
                    }
                    else
                    {
                        OperandType = OpType.Symbolic;
                        Base = Index = Register = null;
                        Scale = 0;
                        Offset = Value = 0;
                    }
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Метод, создающий адресный операнд.
            /// </summary>
            /// <param name="sizeFlag"> Булево значение, задающее размерность адресного операнда (<c>true</c> - 8 бит, <c>false</c> - 16/32 бита). </param>
            private void _createAddressOperand(bool sizeFlag)
            {
                OperandType = sizeFlag ? OpType.Address8 : OpType.Address16_32;
                Value = 0;
                Register = null;
                Offset = 0;
                Scale = 255;
                Base = null;
                Index = null;
                StringOperand = _sumAllNums(StringOperand.Substring(1, StringOperand.Length - 2));
                string[] tmp = StringOperand.Split(new char[] { '+', '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length > 3) { throw new SASMTranslationException("Address cannot be parsed!"); }
                char[] signs = new char[tmp.Length];
                int counter = signs.Length - 1;
                for (int i = StringOperand.Length - 1; i >= 0; i--)
                {
                    if (StringOperand[i] == '-' || StringOperand[i] == '+') { signs[counter--] = StringOperand[i]; }
                }
                for (int i = 0; i < tmp.Length; i++)
                {
                    if (_isConstant(tmp[i]))
                    {
                        Offset = (signs[i] == '-' ? -1 : 1) * tmp[i].GetInt();
                    }
                    else if (tmp[i].Contains('*'))
                    {
                        if (signs[i] == '-') { throw new SASMTranslationException("\"Index\" cannot be used with \"-\"!"); }
                        if (Index != null) { throw new SASMTranslationException("Scale was used twice!"); }
                        string left = tmp[i].Substring(0, tmp[i].IndexOf('*'));
                        string right = tmp[i].Substring(tmp[i].IndexOf('*') + 1);
                        string strScale, strIndex;
                        if (_isConstant(left))
                        {
                            strScale = left;
                            strIndex = right;
                        }
                        else if (_isConstant(right))
                        {
                            strScale = right;
                            strIndex = left;
                        }
                        else
                        {
                            throw new SASMTranslationException("Scale must be a constant!");
                        }
                        int scale = strScale.GetInt();
                        if (scale != 1 && scale != 2 && scale != 4 && scale != 8)
                        {
                            throw new SASMTranslationException("Only 1, 2, 4 or 8 can be used as scale!");
                        }
                        switch (scale)
                        {
                            case 8:
                                Scale = 3;
                                break;
                            case 4:
                                Scale = 2;
                                break;
                            case 2:
                                Scale = 1;
                                break;
                            case 1:
                            default:
                                Scale = 0;
                                break;
                        }
                        if (!Enum.TryParse(strIndex, out Registers regIndex))
                        {
                            throw new SASMTranslationException("Only one of base registers can be used used as \"Index\" register!");
                        }
                        Index = regIndex;
                    }
                    else
                    {

                        if (signs[i] == '-') { throw new SASMTranslationException("\"Base\" cannot be used with \"-\"!"); }
                        if (!Enum.TryParse(tmp[i], out Registers regBase))
                        {
                            throw new SASMTranslationException("Only one of base registers can be used as \"Base\" register!");
                        }
                        if (Base == null) { Base = regBase; }
                        else if (Index == null)
                        {
                            Index = regBase;
                            Scale = 0;
                        }
                        else { throw new SASMTranslationException("Another register has already been used as \"Base\"!"); }
                    }
                }
            }

            /// <summary>
            /// Суммирует все числовые слагаемые в строке с учётом их знаков.
            /// </summary>
            /// <param name="str"> Строка, слагаемые в которой требуется просуммировать. </param>
            /// <returns> Строка <paramref name="str"/>, в которой все числовые слагаемые заменены единственной суммой. </returns>
            private static string _sumAllNums(string str)
            {
                str = str.ToLower().Replace(" ", "");
                int intVal = 0;
                string strVal = "", nonNumStr = "";
                for (int i = 0; i < str.Length; i++)
                {
                    if (str[i] == '-' || str[i] == '+')
                    {
                        if (strVal != "")
                        {
                            if (_isConstant(strVal)) { intVal += strVal.GetInt(); }
                            else { nonNumStr += strVal; }
                        }
                        strVal = "";
                    }
                    strVal += str[i];
                }
                if (_isConstant(strVal)) { intVal += strVal.GetInt(); }
                else { nonNumStr += strVal; }
                nonNumStr = nonNumStr.Trim(new char[] { '+', '-' });
                return nonNumStr + (intVal < 0 ? "" : "+") + Convert.ToString(intVal);
            }

            /// <summary>
            /// Проверяет, что содержимое строки является числовой константой.
            /// Поддерживаются шестнадцатеричные константы, а также константы со знаком.
            /// </summary>
            /// <param name="val"> Строка, которую требуется проверить. </param>
            /// <returns> Булево значение, показывающее, является ли содержимое строки <paramref name="val"/> числовой константой. </returns>
            private static bool _isConstant(string val)
            {
                if (val[0] == '-' || val[0] == '+') { val = val.Substring(1); }
                bool flag = false;
                if (val[val.Length - 1] == 'h' && (val[0] == '0' || char.IsDigit(val[0])))
                {
                    flag = true;
                    for (int i = 0; i < val.Length - 1; i++)
                    {
                        if (!val[i].IsHexDigit())
                        {
                            return false;
                        }
                    }
                }
                return flag || Array.TrueForAll(val.ToCharArray(), (char n) => char.IsDigit(n) || n == 'b' || n == 'd');
            }
        }

        /// <summary>
        /// Класс, описывающий инструкцию ассемблера.
        /// </summary>
        private class Instruction
        {
            /// <summary>
            /// Перечисление, описывающее операнды-приёмники и операнды-источники инструкций.
            /// </summary>
            [Flags]
            private enum TransferType : byte
            {
                to_m16_32 = 0b10000000,
                to_m8 = 0b01000000,
                to_r16_32 = 0b00100000,
                to_r8 = 0b00010000,
                from_m16_32 = 0b00001000,
                from_m8 = 0b00000100,
                from_r16_32 = 0b00000010,
                from_r8 = 0b00000001,
                from_const = 0b00000000,
                from_r = from_r16_32 | from_r8,
                from_m = from_m16_32 | from_m8,
                to_r = to_r16_32 | to_r8,
                to_m = to_m16_32 | to_m8,
                from_8 = from_m8 | from_r8,
                from_16_32 = from_m16_32 | from_r16_32,
                to_8 = to_m8 | to_r8,
                to_16_32 = to_m16_32 | to_r16_32
            }

            /// <summary>
            /// Список двоичных кодов префиксов инструкции.
            /// </summary>
            public List<byte> InstrPrefixes;
            /// <summary>
            /// Список строковых представлений префиксов инструкции.
            /// </summary>
            public List<string> StringInstrPrefixes;
            /// <summary>
            /// Мнемоника машинной команды.
            /// </summary>
            public string Command;
            /// <summary>
            /// Список операндов инструкции.
            /// </summary>
            public List<Operand> Arguments;
            /// <summary>
            /// Смещение инструкции относительно начала SASM-кода.
            /// </summary>
            public int Offset;
            /// <summary>
            /// Двоичный код инструкции.
            /// </summary>
            public byte[] ByteCode;

            /// <summary>
            /// Объект типа <see cref="SASMCode"/>, используемый для парсинга операндов и рассчёта смещения символьных операндов.
            /// </summary>
            private SASMCode _codeObj;
            /// <summary>
            /// Флаг, показывающий, нужно ли использовать в символьных операндах относительное смещение (относительно начала SASM-кода)
            /// или же абсолютное (относительно начала адресного пространства процесса).
            /// </summary>
            private bool _isRelativeOffset;

            /// <summary>
            /// Конструктор, создающий описание инструкции путём парсинга её строкового представления.
            /// </summary>
            /// <param name="CodeObj"> Объект типа <see cref="SASMCode"/>, используемый для парсинга операндов и рассчёта смещения символьных операндов. </param>
            /// <param name="Line"> Строковое представление инструкции. </param>
            public Instruction(SASMCode CodeObj, string Line)
            {
                Line = Line.Trim();
                _codeObj = CodeObj;
                _isRelativeOffset = false;
                Offset = 0;
                ByteCode = null;
                InstrPrefixes = new List<byte>();
                StringInstrPrefixes = new List<string>();
                Arguments = new List<Operand>();
                if (Line.StartsWith("addconst "))
                {
                    Command = Line;
                    ByteCode = new byte[0];
                    string[] tmp = Line.Substring(Line.IndexOf(" ") + 1).Split(',');
                    tmp[0] = tmp[0].Trim();
                    tmp[1] = tmp[1].Trim();
                    try
                    {
                        CodeObj._consts.Add(tmp[0], tmp[1]);
                    }
                    catch (ArgumentException)
                    {
                        throw new SASMTranslationException($"Constant with name \"{tmp[0]}\" was already defined!", this);
                    }
                    return;
                }
                else if (Line.StartsWith("remconst "))
                {
                    Command = Line;
                    ByteCode = new byte[0];
                    string[] tmp = Line.Substring(Line.IndexOf(" ") + 1).Split(',').Select((string s) => s.Trim()).ToArray();
                    foreach (string s in tmp)
                    {
                        try
                        {
                            CodeObj._consts.Remove(s);
                            CodeObj._removedConsts.Add(s);
                        }
                        catch (ArgumentException)
                        {
                            throw new SASMTranslationException($"Constant with name \"{tmp[0]}\" wasn't defined yet!", this);
                        }
                    }
                    return;
                }
                if (Line.Contains("\""))
                {
                    string[] tmp = Line.Split('"');
                    for (int i = 0; i < tmp.Length; i += 2)
                    {
                        tmp[i] = tmp[i].ToLower();
                    }
                    Line = string.Join("\"", tmp);
                }
                else { Line = Line.ToLower(); }
                if (Line.StartsWith("repe ") || Line.StartsWith("repz "))
                {
                    InstrPrefixes.Add((byte)Prefixes.REPE);
                    StringInstrPrefixes.Add("repe");
                    Line = Line.Substring(5);
                }
                else if (Line.StartsWith("repne ") || Line.StartsWith("repnz "))
                {
                    InstrPrefixes.Add((byte)Prefixes.REPNE);
                    StringInstrPrefixes.Add("repne");
                    Line = Line.Substring(6);
                }
                else if (Line.StartsWith("rep "))
                {
                    InstrPrefixes.Add((byte)Prefixes.REPE);
                    StringInstrPrefixes.Add("rep");
                    Line = Line.Substring(4);
                }
                if (Line.IndexOf(' ') != -1)
                {
                    Command = Line.Substring(0, Line.IndexOf(' '));
                    Line = Line.Substring(Line.IndexOf(' ') + 1);
                    List<string> arguments = new List<string>();
                    int i;
                    while ((i = Line.IndexOf(',')) != -1)
                    {
                        arguments.Add(Line.Substring(0, i).Replace(" ", ""));
                        if (Line[i + 1] == ' ')
                        {
                            Line = Line.Substring(i + 2);
                        }
                        else
                        {
                            Line = Line.Substring(i + 1);
                        }
                    }
                    arguments.Add(Line.Replace(" ", ""));
                    bool opSizeFlag = false;
                    foreach (string s in arguments)
                    {
                        if (s == "ax" || s == "bx" || s == "cx" || s == "dx" ||
                            s == "di" || s == "si" || s == "sp" || s == "bp" ||
                            s.StartsWith("word"))
                        {
                            StringInstrPrefixes.Add("word");
                            if (s.Contains("["))
                            {
                                if (s.StartsWith("word") && !opSizeFlag)
                                {
                                    opSizeFlag = true;
                                    InstrPrefixes.Add((byte)Prefixes.OpSize);
                                }
                                else
                                {
                                    InstrPrefixes.Add((byte)Prefixes.AddrSize);
                                }
                            }
                            else if (!opSizeFlag)
                            {
                                opSizeFlag = true;
                                InstrPrefixes.Add((byte)Prefixes.OpSize);
                            }
                        }
                        try
                        {
                            Arguments.Add(new Operand(CodeObj, s));
                        }
                        catch (SASMTranslationException ex)
                        {
                            if (ex.Reason == null)
                            {
                                ex.Reason = this;
                            }
                            throw;
                        }
                    }
                }
                else
                {
                    Command = Line;
                }
                ByteCode = _getByteCode();
            }

            /// <summary>
            /// Возвращает строковое представление инструкции.
            /// </summary>
            /// <returns> Строковое представление инструкции. </returns>
            public override string ToString()
            {
                string strPref = string.Join(" ", StringInstrPrefixes);
                return strPref + (strPref == "" ? "" : " ") + Command + " " + string.Join(", ", Arguments);
            }

            /// <summary>
            /// Проверяет допустимость сочетания операндов, в частности - совпадение размеров приёмника и источника.
            /// </summary>
            /// <param name="transfer1"> Тип операнда-приёмника. </param>
            /// <param name="transfer2"> Тип операнда-источника. </param>
            private void _checkTransfer(TransferType transfer1, TransferType transfer2)
            {
                if (transfer2 != 0 && ((byte)transfer1 >> 4 | (byte)transfer2) == 0)
                {
                    throw new SASMTranslationException("Check operands: propbably size mismatching.", this);
                }
            }

            /// <summary>
            /// Преобразует число в его байтовое представление.
            /// </summary>
            /// <param name="Val"> Число для преобразования. </param>
            /// <param name="AvailibleSizes"> Возможные размеры результата (в байтах). Как правило, используются размеры 8, 16, 32 или их сочетания. </param>
            /// <returns> 
            /// Массив байт длины, допускаемой <paramref name="AvailibleSizes"/>, содержащий байтовое представление <paramref name="Val"/>.
            /// </returns>
            private byte[] _getDWORDVal(int Val, byte AvailibleSizes)
            {
                byte size;
                if ((uint)Val <= byte.MaxValue) { size = 8; }
                else if ((uint)Val <= ushort.MaxValue) { size = 16; }
                else { size = 32; }
                while ((size & AvailibleSizes) == 0 && size <= 32)
                {
                    size <<= 1;
                }
                if (size > 32) { throw new SASMTranslationException("Wrong size of numeric value!", this); }
                string _sval = Convert.ToString(Val, 16).PadLeft(size / 4, '0');
                byte[] result;
                result = new byte[size / 8];
                for (int i = 0; i < size / 8; i++)
                {
                    result[i] = Convert.ToByte(_sval.Substring((size / 4 - 2) - i * 2, 2), 16);
                }
                return result;
            }

            /// <summary>
            /// Возвращает байт ModR/M для данной инструкции, а также (при необходимости) байт SIB и байты смещения.
            /// </summary>
            /// <param name="FirstOnly"> Следует ли использовать только первый операнд двуоперандной инструкциии. </param>
            /// <returns> Массив байт, содержащий ModR/M, а также(опционально) SIB и смещение. </returns>
            /// <seealso cref="_getSIB(Operand)"/>
            private byte[] _getModRM(bool FirstOnly = false)
            {
                byte? mod = null, reg = null, rm = null;
                byte? sib = null;
                byte[] offset = null;
                Operand operand;
                if (Arguments.Count == 2 && !FirstOnly)
                {
                    if ((Arguments[1].OperandType & Operand.OpType.Address) != 0)
                    {
                        if ((Arguments[0].OperandType & Operand.OpType.Address) != 0)
                        {
                            throw new SASMTranslationException("Check operands: probably memory-memory access.", this);
                        }
                        else
                        {
                            reg = (byte)Arguments[0].Register;
                            operand = Arguments[1];
                        }
                    }
                    else
                    {
                        reg = (byte)Arguments[1].Register;
                        operand = Arguments[0];
                    }
                }
                else
                {
                    reg = 0b000;
                    operand = Arguments[0];
                }

                if (operand.Index == Registers.esp)
                {
                    if (operand.Scale != 255 && operand.Scale != 0 || operand.Base == Registers.esp)
                    {
                        throw new SASMTranslationException("ESP cannot be used as \"Index\" register!");
                    }
                    operand.Scale = 0;
                    operand.Index = operand.Base;
                    operand.Base = Registers.esp;
                }
                if ((operand.Scale == 255 || operand.Scale == 0) && operand.Base == Registers.ebp && operand.Index != null)
                {
                    operand.Scale = 0;
                    operand.Base = operand.Index;
                    operand.Index = Registers.ebp;
                }

                if ((operand.OperandType & Operand.OpType.Register) != 0)
                {
                    mod = 0b11;
                    rm = (byte)operand.Register;
                }
                else if (operand.Index == null && operand.Base == null)
                {
                    mod = 0b00;
                    rm = 0b101;
                    offset = _getDWORDVal(operand.Offset, 32);
                }
                else
                {
                    if ((operand.Offset == 0 || operand.Base == null) && operand.Base != Registers.ebp)
                    {
                        mod = 0b00;
                        if (operand.Base == null)
                        {
                            offset = _getDWORDVal(operand.Offset, 32);
                        }
                    }
                    else
                    {
                        offset = _getDWORDVal(operand.Offset, 8 | 32);
                        if (offset.Length == 1)
                        {
                            mod = 0b01;
                        }
                        else
                        {
                            mod = 0b10;
                        }
                    }
                    if (operand.Index == null && operand.Base != Registers.esp && (operand.Offset != 0 || operand.Base != Registers.ebp))
                    {
                        rm = (byte)operand.Base;
                    }
                    else
                    {
                        rm = 0b100;
                        sib = _getSIB(operand);
                    }
                }

                byte[] result = new byte[] { (byte)(mod << 6 | reg << 3 | rm) };
                if (sib != null) { result = result.Add((byte)sib); }
                if (offset != null) { result = result.Add(offset); }
                return result;
            }

            /// <summary>
            /// Возвращает байт SIB для заданного операнда.
            /// </summary>
            /// <param name="operand"> Операнд с типом <see cref="Operand.OperandType"/> = <see cref="Operand.OpType.Address"/>. </param>
            /// <returns> Байт SIB. </returns>
            private byte _getSIB(Operand operand)
            {
                if (operand.Scale == 255 || operand.Index == null)
                {
                    return (byte)(0b100 << 3 | (byte)operand.Base);
                }
                else if (operand.Base == null)
                {
                    return (byte)(operand.Scale << 6 | (byte)operand.Index << 3 | 0b101);
                }
                else
                {
                    return (byte)(operand.Scale << 6 | (byte)operand.Index << 3 | (byte)operand.Base);
                }
            }

            /// <summary>
            /// Используется в качестве callback-а для задания 32-битного смещения или 32-битной числовой константы в инструкциях,
            /// где один из операндов - символьный (<see cref="Operand.OperandType"/> = <see cref="Operand.OpType.Symbolic"/>).
            /// </summary>
            /// <seealso cref="_setOffset8"/>
            private void _setOffset32()
            {
                byte[] offset;
                string label = "";
                foreach (Operand o in Arguments)
                {
                    if (o.OperandType == Operand.OpType.Symbolic)
                    {
                        label = o.StringOperand;
                        break;
                    }
                }
                if (_isRelativeOffset)
                {
                    offset = _getDWORDVal(_getRelativeLabelOffset(label), 32);
                }
                else
                {
                    int l_offset = Label.GetOffset(_codeObj._labels, label);
                    if (l_offset == -1) { throw new SASMTranslationException($"Label \"{label}\" wasn't defined!", this); }
                    offset = _getDWORDVal(l_offset, 32);
                }
                for (int i = 0; i < 4; i++)
                {
                    ByteCode[ByteCode.Length - 4 + i] = offset[i];
                }
            }

            /// <summary>
            /// Используется в качестве callback-а для задания 8-битного смещения или 8-битной числовой константы в инструкциях,
            /// где один из операндов - символьный (<see cref="Operand.OperandType"/> = <see cref="Operand.OpType.Symbolic"/>).
            /// </summary>
            /// <seealso cref="_setOffset32"/>
            private void _setOffset8()
            {
                byte[] offset;
                string label = "";
                foreach (Operand o in Arguments)
                {
                    if (o.OperandType == Operand.OpType.Symbolic)
                    {
                        label = o.StringOperand;
                        break;
                    }
                }
                if (_isRelativeOffset)
                {
                    offset = _getDWORDVal(_getRelativeLabelOffset(label), 8);
                }
                else
                {
                    int l_offset = Label.GetOffset(_codeObj._labels, label);
                    if (l_offset == -1) { throw new SASMTranslationException($"Label \"{label}\" wasn't defined!", this); }
                    offset = _getDWORDVal(l_offset, 8);
                }
                ByteCode[ByteCode.Length - 1] = offset[0];
            }

            /// <summary>
            /// Возвращает двоичный код данной инструкции.
            /// </summary>
            /// <returns> Массив, содержащий байты двоичного машенного кода инструкции. </returns>
            private byte[] _getByteCode()
            {
                #region 0 operands
                if (Arguments.Count == 0)
                {
                    switch (Command)
                    {
                        case "pusha":
                        case "pushad":
                            return new byte[] { 0x60 };
                        case "pushf":
                        case "pushfd":
                            return new byte[] { 0x9C };
                        case "popa":
                        case "popad":
                            return new byte[] { 0x61 };
                        case "popf":
                        case "popfd":
                            return new byte[] { 0x9D };
                        case "ret":
                        case "retn":
                            return new byte[] { 0xC3 };
                        case "retf":
                            return new byte[] { 0xCB };
                        case "nop":
                            return new byte[] { 0x90 };
                        case "cmc":
                            return new byte[] { 0xF5 };
                        case "clc":
                            return new byte[] { 0xF8 };
                        case "stc":
                            return new byte[] { 0xF9 };
                        case "cli":
                            return new byte[] { 0xFA };
                        case "sti":
                            return new byte[] { 0xFB };
                        case "cld":
                            return new byte[] { 0xFC };
                        case "std":
                            return new byte[] { 0xFD };
                        case "int1":
                        case "icebp":
                            return new byte[] { 0xF1 };
                        case "int3":
                            return new byte[] { 0xCC };
                        case "lahf":
                            return new byte[] { 0x9F };
                        case "sahf":
                            return new byte[] { 0x9E };
                        case "cbw":
                            return new byte[] { 0x66, 0x98 };
                        case "cwde":
                            return new byte[] { 0x98 };
                        case "cwd":
                            return new byte[] { 0x66, 0x99 };
                        case "cdq":
                            return new byte[] { 0x99 };
                        case "movs":
                        case "movsb":
                            return new byte[] { 0xA4 };
                        case "movsw":
                            return new byte[] { 0x66, 0xA5 };
                        case "movsd":
                            return new byte[] { 0xA5 };
                        case "cmps":
                        case "cmpsb":
                            return new byte[] { 0xA6 };
                        case "cmpsw":
                            return new byte[] { 0x66, 0xA7 };
                        case "cmpsd":
                            return new byte[] { 0xA7 };
                        case "stos":
                        case "stosb":
                            return new byte[] { 0xAA };
                        case "stosw":
                            return new byte[] { 0x66, 0xAB };
                        case "stosd":
                            return new byte[] { 0xAB };
                        case "lods":
                        case "lodsb":
                            return new byte[] { 0xAC };
                        case "lodsw":
                            return new byte[] { 0x66, 0xAD };
                        case "lodsd":
                            return new byte[] { 0xAD };
                        case "scas":
                        case "scasb":
                            return new byte[] { 0xAE };
                        case "scasw":
                            return new byte[] { 0x66, 0xAF };
                        case "scasd":
                            return new byte[] { 0xAF };
                        case "salc":
                        case "setalc":
                            return new byte[] { 0xD6 };
                        case "xlat":
                        case "xlatb":
                            return new byte[] { 0xD7 };
                        default:
                            throw new SASMTranslationException("Unknown command or incorrect number of operands!", this);
                    }
                }
                #endregion
                try
                {
                    #region 1 operand
                    if (Arguments.Count == 1)
                    {
                        TransferType transfer = TransferType.from_const;
                        bool isSymbolic = false;
                        switch (Arguments[0].OperandType)
                        {
                            case Operand.OpType.Symbolic:
                                if (Command != "push" && !Command.StartsWith("j") && !Command.StartsWith("loop") && Command != "call")
                                {
                                    throw new SASMTranslationException($"Symbolic operand is not valid for \"{Command}\"", this);
                                }
                                isSymbolic = true;
                                transfer = TransferType.from_const;
                                break;
                            case Operand.OpType.Const:
                                transfer = TransferType.from_const;
                                break;
                            case Operand.OpType.Address8:
                                transfer = TransferType.from_m8;
                                break;
                            case Operand.OpType.Address16_32:
                                transfer = TransferType.from_m16_32;
                                break;
                            case Operand.OpType.Register8:
                                transfer = TransferType.from_r8;
                                break;
                            case Operand.OpType.Register16_32:
                                transfer = TransferType.from_r16_32;
                                break;
                        }
                        byte[] result = new byte[0];
                        byte[] _const;
                        switch (Command)
                        {
                            case "push":
                                if (transfer == TransferType.from_const)
                                {
                                    if (isSymbolic)
                                    {
                                        _isRelativeOffset = false;
                                        _codeObj._labelOffsetsComputed += _setOffset32;
                                    }
                                    _const = _getDWORDVal(Arguments[0].Value, (byte)((isSymbolic ? 0 : 8) | 32));
                                    if (_const.Length == 1) { return ((byte)0x6A).Add(_const); }
                                    else { return ((byte)0x68).Add(_const); }
                                }
                                else if ((transfer & TransferType.from_r) != 0)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { (byte)(0x50 | regCode) };
                                }
                                result = ((byte)0xFF).Add(_getModRM());
                                result[1] |= 0b00110000;
                                return result;
                            case "pop":
                                if (transfer == TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"pop\" instruction can't be constant!", this);
                                }
                                else if ((transfer & TransferType.from_8) != 0)
                                {
                                    throw new SASMTranslationException("Operand for \"pop\" command can't be 8-bit!", this);
                                }
                                else if ((transfer & TransferType.from_r) != 0)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { (byte)(0x58 | regCode) };
                                }
                                result = ((byte)0x8F).Add(_getModRM());
                                return result;
                            case "inc":
                                if (transfer == TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"inc\" instruction can't be constant!", this);
                                }
                                else if (transfer == TransferType.from_r16_32)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { (byte)(0x40 | regCode) };
                                }
                                else if (transfer == TransferType.from_r8)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { 0xFE, (byte)(0xC0 | regCode) };
                                }
                                else if (transfer == TransferType.from_m8)
                                {
                                    result = new byte[] { 0xFE };
                                }
                                else
                                {
                                    result = new byte[] { 0xFF };
                                }
                                return result.Add(_getModRM());
                            case "dec":
                                if (transfer == TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"dec\" instruction can't be constant!", this);
                                }
                                else if (transfer == TransferType.from_r16_32)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { (byte)(0x48 | regCode) };
                                }
                                else if (transfer == TransferType.from_r8)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { 0xFE, (byte)(0xC8 | regCode) };
                                }
                                else if (transfer == TransferType.from_m8)
                                {
                                    result = new byte[] { 0xFE };
                                }
                                else
                                {
                                    result = new byte[] { 0xFF };
                                }
                                result = result.Add(_getModRM());
                                result[1] |= 0b00001000;
                                return result;
                            case "ret":
                            case "retn":
                                if (transfer != TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"ret\" instruction must be constant!", this);
                                }
                                return ((byte)0xC2).Add(_getDWORDVal(Arguments[0].Value, 32));
                            case "retf":
                                if (transfer != TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"ret\" instruction must be constant!", this);
                                }
                                return ((byte)0xCA).Add(_getDWORDVal(Arguments[0].Value, 32));
                            case "int":
                                if (transfer != TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"int\" instruction must be constant!", this);
                                }
                                result = new byte[] { 0xCD };
                                byte[] opVal = _getDWORDVal(Arguments[0].Value, 8);
                                return result.Add(opVal);
                            case "loopnz":
                            case "loopne":
                                if (transfer != TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"loopnz\" instruction must be constant!", this);
                                }
                                if (isSymbolic)
                                {
                                    _isRelativeOffset = true;
                                    _codeObj._labelOffsetsComputed += _setOffset8;
                                }
                                _const = _getDWORDVal(Arguments[0].Value, 8);
                                return ((byte)0xE0).Add(_const);
                            case "loopz":
                            case "loope":
                                if (transfer != TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"loopz\" instruction must be constant!", this);
                                }
                                if (isSymbolic)
                                {
                                    _isRelativeOffset = true;
                                    _codeObj._labelOffsetsComputed += _setOffset8;
                                }
                                _const = _getDWORDVal(Arguments[0].Value, 8);
                                return ((byte)0xE1).Add(_const);
                            case "loop":
                                if (transfer != TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"loop\" instruction must be constant!", this);
                                }
                                if (isSymbolic)
                                {
                                    _isRelativeOffset = true;
                                    _codeObj._labelOffsetsComputed += _setOffset8;
                                }
                                _const = _getDWORDVal(Arguments[0].Value, 8);
                                return ((byte)0xE2).Add(_const);
                            case "in":
                                if (transfer != TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"in\" instruction must be constant!", this);
                                }
                                result = new byte[] { 0xE5 };
                                opVal = _getDWORDVal(Arguments[0].Value, 8);
                                return result.Add(opVal);
                            case "out":
                                if (transfer != TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"out\" instruction must be constant!", this);
                                }
                                result = new byte[] { 0xE7 };
                                opVal = _getDWORDVal(Arguments[0].Value, 8);
                                return result.Add(opVal);
                            case "call":
                                if (transfer == TransferType.from_const)
                                {
                                    if (isSymbolic)
                                    {
                                        _isRelativeOffset = true;
                                        _codeObj._labelOffsetsComputed += _setOffset32;
                                    }
                                    _const = _getDWORDVal(Arguments[0].Value, 32);
                                    return ((byte)0xE8).Add(_const);
                                }
                                else if ((transfer & TransferType.from_r) != 0)
                                {
                                    result = ((byte)0xFF).Add(_getModRM());
                                    result[1] |= 0b00010000;
                                    return result;
                                }
                                else
                                {
                                    result = ((byte)0xFF).Add(_getModRM());
                                    result[1] |= 0b00011000;
                                    return result;
                                }
                            case "not":
                                if (transfer == TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"not\" instruction can't be constant!", this);
                                }
                                else if (transfer == TransferType.from_r16_32)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { 0xF7, (byte)(0xD0 | regCode) };
                                }
                                else if (transfer == TransferType.from_r8)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { 0xF6, (byte)(0xD0 | regCode) };
                                }
                                else if (transfer == TransferType.from_m8)
                                {
                                    result = new byte[] { 0xF6 };
                                }
                                else
                                {
                                    result = new byte[] { 0xF7 };
                                }
                                result = result.Add(_getModRM());
                                result[1] |= 0b00010000;
                                return result;
                            case "neg":
                                if (transfer == TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"neg\" instruction can't be constant!", this);
                                }
                                else if (transfer == TransferType.from_r16_32)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { 0xF7, (byte)(0xD8 | regCode) };
                                }
                                else if (transfer == TransferType.from_r8)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { 0xF6, (byte)(0xD8 | regCode) };
                                }
                                else if (transfer == TransferType.from_m8)
                                {
                                    result = new byte[] { 0xF6 };
                                }
                                else
                                {
                                    result = new byte[] { 0xF7 };
                                }
                                result = result.Add(_getModRM());
                                result[1] |= 0b00011000;
                                return result;
                            case "mul":
                                if (transfer == TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"mul\" instruction can't be constant!", this);
                                }
                                else if (transfer == TransferType.from_r16_32)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { 0xF7, (byte)(0xE0 | regCode) };
                                }
                                else if (transfer == TransferType.from_r8)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { 0xF6, (byte)(0xE0 | regCode) };
                                }
                                else if (transfer == TransferType.from_m8)
                                {
                                    result = new byte[] { 0xF6 };
                                }
                                else
                                {
                                    result = new byte[] { 0xF7 };
                                }
                                result = result.Add(_getModRM());
                                result[1] |= 0b00100000;
                                return result;
                            case "imul":
                                if (transfer == TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"imul\" instruction can't be constant!", this);
                                }
                                else if (transfer == TransferType.from_r16_32)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { 0xF7, (byte)(0xE8 | regCode) };
                                }
                                else if (transfer == TransferType.from_r8)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { 0xF6, (byte)(0xE8 | regCode) };
                                }
                                else if (transfer == TransferType.from_m8)
                                {
                                    result = new byte[] { 0xF6 };
                                }
                                else
                                {
                                    result = new byte[] { 0xF7 };
                                }
                                result = result.Add(_getModRM());
                                result[1] |= 0b00101000;
                                return result;
                            case "div":
                                if (transfer == 0)
                                {
                                    throw new SASMTranslationException("Operand in \"div\" instruction can't be constant!", this);
                                }
                                else if (transfer == TransferType.from_r16_32)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { 0xF7, (byte)(0xF0 | regCode) };
                                }
                                else if (transfer == TransferType.from_r8)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { 0xF6, (byte)(0xF0 | regCode) };
                                }
                                else if (transfer == TransferType.from_m8)
                                {
                                    result = new byte[] { 0xF6 };
                                }
                                else
                                {
                                    result = new byte[] { 0xF7 };
                                }
                                result = result.Add(_getModRM());
                                result[1] |= 0b00110000;
                                return result;
                            case "idiv":
                                if (transfer == 0)
                                {
                                    throw new SASMTranslationException("Operand in \"idiv\" instruction can't be constant!", this);
                                }
                                else if (transfer == TransferType.from_r16_32)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { 0xF7, (byte)(0xF8 | regCode) };
                                }
                                else if (transfer == TransferType.from_r8)
                                {
                                    byte regCode = (byte)Arguments[0].Register;
                                    return new byte[] { 0xF6, (byte)(0xF8 | regCode) };
                                }
                                else if (transfer == TransferType.from_m8)
                                {
                                    result = new byte[] { 0xF6 };
                                }
                                else
                                {
                                    result = new byte[] { 0xF7 };
                                }
                                result = result.Add(_getModRM());
                                result[1] |= 0b00111000;
                                return result;
                            case "storeb":
                                return _getDWORDVal(Arguments[0].Value, 8);
                            case "storew":
                                return _getDWORDVal(Arguments[0].Value, 16);
                            case "stored":
                                return _getDWORDVal(Arguments[0].Value, 32);
                            case "jmp":
                                if (transfer == TransferType.from_const)
                                {
                                    if (isSymbolic)
                                    {
                                        _isRelativeOffset = true;
                                        _codeObj._labelOffsetsComputed += _setOffset8;
                                    }
                                    _const = _getDWORDVal(Arguments[0].Value, 8 | 32);
                                    if (_const.Length == 1) { return ((byte)0xEB).Add(_const); }
                                    else { return ((byte)0xE9).Add(_const); }
                                }
                                else if ((transfer & TransferType.from_r) != 0)
                                {
                                    result = ((byte)0xFF).Add(_getModRM());
                                    result[1] |= 0b00100000;
                                    return result;
                                }
                                else
                                {
                                    result = ((byte)0xFF).Add(_getModRM());
                                    result[1] |= 0b00101000;
                                    return result;
                                }
                            case "jcxz":
                            case "jecxz":
                                if (transfer != TransferType.from_const)
                                {
                                    throw new SASMTranslationException("Operand in \"jczx\" instruction must be constant!", this);
                                }
                                if (isSymbolic)
                                {
                                    _isRelativeOffset = true;
                                    _codeObj._labelOffsetsComputed += _setOffset8;
                                }
                                _const = _getDWORDVal(Arguments[0].Value, 8);
                                return ((byte)0xE3).Add(_const);
                            default:
                                if (Enum.TryParse(Command, out Jumps jmp))
                                {
                                    if (transfer != TransferType.from_const)
                                    {
                                        throw new SASMTranslationException($"Operand in \"{Command}\" instruction must be constant!", this);
                                    }
                                    if (isSymbolic)
                                    {
                                        _isRelativeOffset = true;
                                        _codeObj._labelOffsetsComputed += _setOffset32;
                                    }
                                    _const = _getDWORDVal(Arguments[0].Value, 32);
                                    return new byte[] { 0x0F, (byte)jmp }.Add(_const);
                                }
                                throw new SASMTranslationException("Unknown command or incorrect number of operands!", this);
                        }
                    }
                    #endregion
                    #region 2 operands
                    if (Arguments.Count == 2)
                    {
                        TransferType transfer1 = TransferType.from_const, transfer2 = TransferType.from_const;
                        switch (Arguments[0].OperandType)
                        {
                            case Operand.OpType.Const:
                                throw new SASMTranslationException("First operand in 2-operand command can't be constant!", this);
                            case Operand.OpType.Address8:
                                transfer1 = TransferType.to_m8;
                                break;
                            case Operand.OpType.Address16_32:
                                transfer1 = TransferType.to_m16_32;
                                break;
                            case Operand.OpType.Register8:
                                transfer1 = TransferType.to_r8;
                                break;
                            case Operand.OpType.Register16_32:
                                transfer1 = TransferType.to_r16_32;
                                break;
                        }
                        bool isSymbolic = false;
                        switch (Arguments[1].OperandType)
                        {
                            case Operand.OpType.Symbolic:
                                if (Command != "mov" && Command != "add" && Command != "sub")
                                {
                                    throw new SASMTranslationException($"Symbolic operand is not valid for \"{Command}\"", this);
                                }
                                isSymbolic = true;
                                transfer2 = TransferType.from_const;
                                break;
                            case Operand.OpType.Const:
                                transfer2 = TransferType.from_const;
                                break;
                            case Operand.OpType.Register8:
                                transfer2 = TransferType.from_r8;
                                break;
                            case Operand.OpType.Register16_32:
                                transfer2 = TransferType.from_r16_32;
                                break;
                            case Operand.OpType.Address8:
                                transfer2 = TransferType.from_m8;
                                break;
                            case Operand.OpType.Address16_32:
                                transfer2 = TransferType.from_m16_32;
                                break;
                        }
                        byte[] result = new byte[0];
                        byte[] _const = new byte[0];
                        switch (Command)
                        {
                            case "add":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_r8)
                                {
                                    return ((byte)0x00).Add(_getModRM());
                                }
                                else if ((transfer1 | TransferType.to_16_32) != 0 && transfer2 == TransferType.from_r16_32)
                                {
                                    return ((byte)0x01).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r8 && (transfer2 & TransferType.from_8) != 0)
                                {
                                    return ((byte)0x02).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r16_32 && (transfer2 & TransferType.from_16_32) != 0)
                                {
                                    return ((byte)0x03).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_const)
                                {
                                    if (isSymbolic)
                                    {
                                        if (isSymbolic)
                                        {
                                            _isRelativeOffset = false;
                                            _codeObj._labelOffsetsComputed += _setOffset8;
                                        }
                                    }
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    return ((byte)0x80).Add(_getModRM(true)).Add(_const);
                                }
                                else //if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == 0)
                                {
                                    if (isSymbolic)
                                    {
                                        _isRelativeOffset = false;
                                        _codeObj._labelOffsetsComputed += _setOffset32;
                                    }
                                    _const = _getDWORDVal(Arguments[1].Value, (byte)((isSymbolic ? 0 : 8) | 32));
                                    if (_const.Length == 4)
                                    {
                                        return ((byte)0x81).Add(_getModRM(true)).Add(_const);
                                    }
                                    else
                                    {
                                        return ((byte)0x83).Add(_getModRM(true)).Add(_const);
                                    }
                                }
                            case "or":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_r8)
                                {
                                    return ((byte)0x08).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == TransferType.from_r16_32)
                                {
                                    return ((byte)0x09).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r8 && (transfer2 & TransferType.from_8) != 0)
                                {
                                    return ((byte)0x0A).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r16_32 && (transfer2 & TransferType.from_16_32) != 0)
                                {
                                    return ((byte)0x0B).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_const)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    result = ((byte)0x80).Add(_getModRM(true)).Add(_const);
                                    result[1] |= 0b00001000;
                                    return result;
                                }
                                else //if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == 0)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8 | 32);
                                    if (_const.Length == 4)
                                    {
                                        result = ((byte)0x81).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00001000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0x83).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00001000;
                                        return result;
                                    }
                                }
                            case "adc":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_r8)
                                {
                                    return ((byte)0x10).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == TransferType.from_r16_32)
                                {
                                    return ((byte)0x11).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r8 && (transfer2 & TransferType.from_8) != 0)
                                {
                                    return ((byte)0x12).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r16_32 && (transfer2 & TransferType.from_16_32) != 0)
                                {
                                    return ((byte)0x13).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_const)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    result = ((byte)0x80).Add(_getModRM(true)).Add(_const);
                                    result[1] |= 0b00010000;
                                    return result;
                                }
                                else //if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == 0)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8 | 32);
                                    if (_const.Length == 4)
                                    {
                                        result = ((byte)0x81).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00010000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0x83).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00010000;
                                        return result;
                                    }
                                }
                            case "sbb":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_r8)
                                {
                                    return ((byte)0x18).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == TransferType.from_r16_32)
                                {
                                    return ((byte)0x19).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r8 && (transfer2 & TransferType.from_8) != 0)
                                {
                                    return ((byte)0x1A).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r16_32 && (transfer2 & TransferType.from_16_32) != 0)
                                {
                                    return ((byte)0x1B).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_const)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    result = ((byte)0x80).Add(_getModRM(true)).Add(_const);
                                    result[1] |= 0b00001100;
                                    return result;
                                }
                                else //if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == 0)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8 | 32);
                                    if (_const.Length == 4)
                                    {
                                        result = ((byte)0x81).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00011000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0x83).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00011000;
                                        return result;
                                    }
                                }
                            case "and":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_r8)
                                {
                                    return ((byte)0x20).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == TransferType.from_r16_32)
                                {
                                    return ((byte)0x21).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r8 && (transfer2 & TransferType.from_8) != 0)
                                {
                                    return ((byte)0x22).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r16_32 && (transfer2 & TransferType.from_16_32) != 0)
                                {
                                    return ((byte)0x23).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_const)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    result = ((byte)0x80).Add(_getModRM(true)).Add(_const);
                                    result[1] |= 0b00100000;
                                    return result;
                                }
                                else //if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == 0)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8 | 32);
                                    if (_const.Length == 4)
                                    {
                                        result = ((byte)0x81).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00100000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0x83).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00100000;
                                        return result;
                                    }
                                }
                            case "sub":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_r8)
                                {
                                    return ((byte)0x28).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == TransferType.from_r16_32)
                                {
                                    return ((byte)0x29).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r8 && (transfer2 & TransferType.from_8) != 0)
                                {
                                    return ((byte)0x2A).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r16_32 && (transfer2 & TransferType.from_16_32) != 0)
                                {
                                    return ((byte)0x2B).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_const)
                                {
                                    if (isSymbolic)
                                    {
                                        _isRelativeOffset = false;
                                        _codeObj._labelOffsetsComputed += _setOffset8;
                                    }
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    result = ((byte)0x80).Add(_getModRM(true)).Add(_const);
                                    result[1] |= 0b00101000;
                                    return result;
                                }
                                else //if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == 0)
                                {
                                    if (isSymbolic)
                                    {
                                        _isRelativeOffset = false;
                                        _codeObj._labelOffsetsComputed += _setOffset32;
                                    }
                                    _const = _getDWORDVal(Arguments[1].Value, (byte)((isSymbolic ? 0 : 8) | 32));
                                    if (_const.Length == 4)
                                    {
                                        result = ((byte)0x81).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00101000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0x83).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00101000;
                                        return result;
                                    }
                                }
                            case "xor":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_r8)
                                {
                                    return ((byte)0x30).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == TransferType.from_r16_32)
                                {
                                    return ((byte)0x31).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r8 && (transfer2 & TransferType.from_8) != 0)
                                {
                                    return ((byte)0x32).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r16_32 && (transfer2 & TransferType.from_16_32) != 0)
                                {
                                    return ((byte)0x33).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_const)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    result = ((byte)0x80).Add(_getModRM(true)).Add(_const);
                                    result[1] |= 0b00110000;
                                    return result;
                                }
                                else //if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == 0)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8 | 32);
                                    if (_const.Length == 4)
                                    {
                                        result = ((byte)0x81).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00110000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0x83).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00110000;
                                        return result;
                                    }
                                }
                            case "cmp":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_r8)
                                {
                                    return ((byte)0x38).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == TransferType.from_r16_32)
                                {
                                    return ((byte)0x39).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r8 && (transfer2 & TransferType.from_8) != 0)
                                {
                                    return ((byte)0x3A).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r16_32 && (transfer2 & TransferType.from_16_32) != 0)
                                {
                                    return ((byte)0x3B).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_const)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    result = ((byte)0x80).Add(_getModRM(true)).Add(_const);
                                    result[1] |= 0b00111000;
                                    return result;
                                }
                                else //if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == 0)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8 | 32);
                                    if (_const.Length == 4)
                                    {
                                        result = ((byte)0x81).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00111000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0x83).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00111000;
                                        return result;
                                    }
                                }
                            case "test":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_r8)
                                {
                                    return ((byte)0x84).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == TransferType.from_r16_32)
                                {
                                    return ((byte)0x85).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_const)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    return ((byte)0xF6).Add(_getModRM(true)).Add(_const);
                                }
                                else if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == TransferType.from_const)
                                {
                                    return ((byte)0xF7).Add(_getModRM(true)).Add(_getDWORDVal(Arguments[1].Value, 32));
                                }
                                else
                                {
                                    throw new SASMTranslationException("Command \"test\" does not support that set of operands!", this);
                                }
                            case "xchg":
                                _checkTransfer(transfer1, transfer2);
                                if (transfer1 == TransferType.to_r8 && (transfer2 & TransferType.from_8) != 0 ||
                                    transfer2 == TransferType.from_r8 && (transfer1 & TransferType.to_8) != 0)
                                {
                                    return ((byte)0x86).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r16_32 && (transfer2 & TransferType.from_16_32) != 0 ||
                                         transfer2 == TransferType.from_r16_32 && (transfer1 & TransferType.to_16_32) != 0)
                                {
                                    return ((byte)0x87).Add(_getModRM());
                                }
                                else
                                {
                                    throw new SASMTranslationException("Command \"xchg\" does not support that set of operands!", this);
                                }
                            case "mov":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_r8)
                                {
                                    return ((byte)0x88).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == TransferType.from_r16_32)
                                {
                                    return ((byte)0x89).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r8 && (transfer2 & TransferType.from_8) != 0)
                                {
                                    return ((byte)0x8A).Add(_getModRM());
                                }
                                else if (transfer1 == TransferType.to_r16_32 && (transfer2 & TransferType.from_16_32) != 0)
                                {
                                    return ((byte)0x8B).Add(_getModRM());
                                }
                                else if ((transfer1 & TransferType.to_8) != 0 && transfer2 == TransferType.from_const)
                                {
                                    if (isSymbolic)
                                    {
                                        _isRelativeOffset = false;
                                        _codeObj._labelOffsetsComputed += _setOffset8;
                                    }
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    return ((byte)0xC6).Add(_getModRM(true)).Add(_const);
                                }
                                else if ((transfer1 & TransferType.to_16_32) != 0 && transfer2 == TransferType.from_const)
                                {
                                    if (isSymbolic)
                                    {
                                        _isRelativeOffset = false;
                                        _codeObj._labelOffsetsComputed += _setOffset32;
                                    }
                                    return ((byte)0xC7).Add(_getModRM(true)).Add(_getDWORDVal(Arguments[1].Value, 32));
                                }
                                else
                                {
                                    throw new SASMTranslationException("Command \"mov\" does not support that set of operands!", this);
                                }
                            case "lea":
                                _checkTransfer(transfer1, transfer2);
                                if (transfer1 == TransferType.to_r16_32 && (transfer2 & TransferType.from_m) != 0)
                                {
                                    return ((byte)0x8D).Add(_getModRM());
                                }
                                else
                                {
                                    throw new SASMTranslationException("Command \"lea\" does not support that set of operands!", this);
                                }
                            case "rol":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_r) == 0)
                                {
                                    throw new SASMTranslationException("First operand of the command \"rol\" musl be register!", this);
                                }
                                if (transfer2 == TransferType.from_const)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    if (transfer1 == TransferType.to_r8)
                                    {
                                        return ((byte)0xC0).Add(_getModRM(true)).Add(_const);
                                    }
                                    else
                                    {
                                        return ((byte)0xC1).Add(_getModRM(true)).Add(_const);
                                    }
                                }
                                else if (transfer2 == TransferType.from_r8 && Arguments[1].Register == Registers.cl)
                                {
                                    if ((transfer1 & TransferType.to_8) != 0)
                                    {
                                        return ((byte)0xD2).Add(_getModRM(true));
                                    }
                                    else
                                    {
                                        return ((byte)0xD3).Add(_getModRM(true));
                                    }
                                }
                                else
                                {
                                    throw new SASMTranslationException("Second operand of the command \"rol\" " +
                                                                   "musl be constant or \"cl\" register!", this);
                                }
                            case "ror":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_r) == 0)
                                {
                                    throw new SASMTranslationException("First operand of the command \"ror\" musl be register!", this);
                                }
                                if (transfer2 == TransferType.from_const)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    if (transfer1 == TransferType.to_r8)
                                    {
                                        result = ((byte)0xC0).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00001000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0xC1).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00001000;
                                        return result;
                                    }
                                }
                                else if (transfer2 == TransferType.from_r8 && Arguments[1].Register == Registers.cl)
                                {
                                    if ((transfer1 & TransferType.to_8) != 0)
                                    {
                                        result = ((byte)0xD2).Add(_getModRM(true));
                                        result[1] |= 0b00001000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0xD3).Add(_getModRM(true));
                                        result[1] |= 0b00001000;
                                        return result;
                                    }
                                }
                                else
                                {
                                    throw new SASMTranslationException("Second operand of the command \"ror\" " +
                                                                   "musl be constant or \"cl\" register!", this);
                                }
                            case "rcl":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_r) == 0)
                                {
                                    throw new SASMTranslationException("First operand of the command \"rcl\" musl be register!", this);
                                }
                                if (transfer2 == TransferType.from_const)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    if (transfer1 == TransferType.to_r8)
                                    {
                                        result = ((byte)0xC0).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00010000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0xC1).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00010000;
                                        return result;
                                    }
                                }
                                else if (transfer2 == TransferType.from_r8 && Arguments[1].Register == Registers.cl)
                                {
                                    if ((transfer1 & TransferType.to_8) != 0)
                                    {
                                        result = ((byte)0xD2).Add(_getModRM(true));
                                        result[1] |= 0b00010000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0xD3).Add(_getModRM(true));
                                        result[1] |= 0b00010000;
                                        return result;
                                    }
                                }
                                else
                                {
                                    throw new SASMTranslationException("Second operand of the command \"rcl\" " +
                                                                   "musl be constant or \"cl\" register!", this);
                                }
                            case "rcr":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_r) == 0)
                                {
                                    throw new SASMTranslationException("First operand of the command \"rcr\" musl be register!", this);
                                }
                                if (transfer2 == TransferType.from_const)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    if (transfer1 == TransferType.to_r8)
                                    {
                                        result = ((byte)0xC0).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00011000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0xC1).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00011000;
                                        return result;
                                    }
                                }
                                else if (transfer2 == TransferType.from_r8 && Arguments[1].Register == Registers.cl)
                                {
                                    if ((transfer1 & TransferType.to_8) != 0)
                                    {
                                        result = ((byte)0xD2).Add(_getModRM(true));
                                        result[1] |= 0b00011000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0xD3).Add(_getModRM(true));
                                        result[1] |= 0b00011000;
                                        return result;
                                    }
                                }
                                else
                                {
                                    throw new SASMTranslationException("Second operand of the command \"rcr\" " +
                                                                   "musl be constant or \"cl\" register!", this);
                                }
                            case "shl":
                            case "sal":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_r) == 0)
                                {
                                    throw new SASMTranslationException("First operand of the command \"sal\" musl be register!", this);
                                }
                                if (transfer2 == TransferType.from_const)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    if (transfer1 == TransferType.to_r8)
                                    {
                                        result = ((byte)0xC0).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00100000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0xC1).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00100000;
                                        return result;
                                    }
                                }
                                else if (transfer2 == TransferType.from_r8 && Arguments[1].Register == Registers.cl)
                                {
                                    if ((transfer1 & TransferType.to_8) != 0)
                                    {
                                        result = ((byte)0xD2).Add(_getModRM(true));
                                        result[1] |= 0b00100000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0xD3).Add(_getModRM(true));
                                        result[1] |= 0b00100000;
                                        return result;
                                    }
                                }
                                else
                                {
                                    throw new SASMTranslationException("Second operand of the command \"sal\" " +
                                                                   "musl be constant or \"cl\" register!", this);
                                }
                            case "shr":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_r) == 0)
                                {
                                    throw new SASMTranslationException("First operand of the command \"shr\" musl be register!", this);
                                }
                                if (transfer2 == TransferType.from_const)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    if (transfer1 == TransferType.to_r8)
                                    {
                                        result = ((byte)0xC0).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00101000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0xC1).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00101000;
                                        return result;
                                    }
                                }
                                else if (transfer2 == TransferType.from_r8 && Arguments[1].Register == Registers.cl)
                                {
                                    if ((transfer1 & TransferType.to_8) != 0)
                                    {
                                        result = ((byte)0xD2).Add(_getModRM(true));
                                        result[1] |= 0b00101000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0xD3).Add(_getModRM(true));
                                        result[1] |= 0b00101000;
                                        return result;
                                    }
                                }
                                else
                                {
                                    throw new SASMTranslationException("Second operand of the command \"shr\" " +
                                                                   "musl be constant or \"cl\" register!", this);
                                }
                            case "sar":
                                _checkTransfer(transfer1, transfer2);
                                if ((transfer1 & TransferType.to_r) == 0)
                                {
                                    throw new SASMTranslationException("First operand of the command \"sar\" musl be register!", this);
                                }
                                if (transfer2 == TransferType.from_const)
                                {
                                    _const = _getDWORDVal(Arguments[1].Value, 8);
                                    if (transfer1 == TransferType.to_r8)
                                    {
                                        result = ((byte)0xC0).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00111000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0xC1).Add(_getModRM(true)).Add(_const);
                                        result[1] |= 0b00111000;
                                        return result;
                                    }
                                }
                                else if (transfer2 == TransferType.from_r8 && Arguments[1].Register == Registers.cl)
                                {
                                    if ((transfer1 & TransferType.to_8) != 0)
                                    {
                                        result = ((byte)0xD2).Add(_getModRM(true));
                                        result[1] |= 0b00111000;
                                        return result;
                                    }
                                    else
                                    {
                                        result = ((byte)0xD3).Add(_getModRM(true));
                                        result[1] |= 0b00111000;
                                        return result;
                                    }
                                }
                                else
                                {
                                    throw new SASMTranslationException("Second operand of the command \"sar\" " +
                                                                   "musl be constant or \"cl\" register!", this);
                                }
                            case "imul":
                                if (transfer1 != TransferType.to_r16_32 || transfer2 != TransferType.from_16_32)
                                {
                                    throw new SASMTranslationException("Command \"imul\" does not support that set of operands!", this);
                                }
                                return new byte[] { 0x0F, 0xAF }.Add(_getModRM());
                            case "movzx":
                                _checkTransfer(transfer1, transfer2);
                                if (transfer1 != TransferType.to_r16_32)
                                {
                                    throw new SASMTranslationException("First operand of the command \"movzx\" must be 16-bit or 32-bit register!", this);
                                }
                                if ((transfer2 & TransferType.from_8) != 0)
                                {
                                    result = new byte[] { 0x0F, 0xB6 }.Add(_getModRM(true));
                                    result[2] |= (byte)((byte)Arguments[0].Register << 3);
                                    return result;
                                }
                                else if ((transfer2 & TransferType.from_16_32) != 0)
                                {
                                    return new byte[] { 0x0F, 0xB7 }.Add(_getModRM());
                                }
                                else
                                {
                                    throw new SASMTranslationException("Command \"movzx\" does not support that set of operands!", this);
                                }
                            default:
                                throw new SASMTranslationException("Unknown command or incorrect number of operands!", this);
                        }
                    }
                    #endregion
                    #region 3 operands
                    if (Arguments.Count == 3)
                    {
                        if (Command == "imul")
                        {
                            if (Arguments[2].OperandType != Operand.OpType.Const)
                            {
                                throw new SASMTranslationException("Third operand in 3-operand command \"imul\" must be constant!", this);
                            }
                            if ((Arguments[0].OperandType & Operand.OpType.Register) == 0)
                            {
                                throw new SASMTranslationException("First operand in 3-operand command \"imul\" must be register!", this);
                            }
                            if (Arguments[1].OperandType == Operand.OpType.Const)
                            {
                                throw new SASMTranslationException("Second operand in 3-operand command \"imul\" can't be constant!", this);
                            }
                            byte[] _const = _getDWORDVal(Arguments[2].Value, 8 | 32);
                            if (_const.Length == 1)
                            {
                                return ((byte)0x6B).Add(_getModRM()).Add(_const);
                            }
                            else
                            {
                                return ((byte)0x69).Add(_getModRM()).Add(_const);
                            }
                        }
                        else
                        {
                            throw new SASMTranslationException("Unknown command or incorrect number of operands!", this);
                        }
                    }
                    #endregion
                }
                catch (SASMTranslationException ex)
                {
                    if (ex.Reason == null)
                    {
                        ex.Reason = this;
                    }
                    throw;
                }
                throw new SASMTranslationException("Can't translate this line.", this);
            }

            /// <summary>
            /// Возвращает смещение метки относительно данной инструкции.
            /// </summary>
            /// <param name="LabelName"> Имя метки, смещение которой требуется вычислить. </param>
            /// <returns> Относительное смещение метки с именем <paramref name="LabelName"/>. </returns>
            private int _getRelativeLabelOffset(string LabelName)
            {
                int l_offset = Label.GetOffset(_codeObj._labels, LabelName);
                if (l_offset == -1) { throw new SASMTranslationException($"Label \"{LabelName}\" wasn't defined!", this); }
                return l_offset - Offset - ByteCode.Length - InstrPrefixes.Count;
            }
        }

        /// <summary>
        /// Класс, описывающий метку.
        /// </summary>
        private class Label
        {
            /// <summary>
            /// Имя метки.
            /// </summary>
            public string Name;
            /// <summary>
            /// Смещение относительно начала SASM-кода в командах.
            /// </summary>
            public int CommandOffset;
            /// <summary>
            /// Смещение относительно начала SASM-кода в байтах.
            /// </summary>
            public int Offset = -1;

            /// <summary>
            /// Конструктор, создающий метку с заданным именем и заданным смещением в командах.
            /// </summary>
            /// <param name="Name"> Имя метки. </param>
            /// <param name="CommandOffset"> Смещение относительно начала SASM-кода в командах. </param>
            public Label(string Name, int CommandOffset)
            {
                this.Name = Name;
                this.CommandOffset = CommandOffset;
            }

            /// <summary>
            /// Возвращает смещение в байтах метки с заданным именем, выбираемой из заданного набора меток.
            /// </summary>
            /// <param name="Labels"> Набор меток, в котором ищется метка с именем <paramref name="Name"/>. </param>
            /// <param name="Name"> Имя метки, смещение которой требуется получить. </param>
            /// <returns> Смещение метки <paramref name="Name"/> относительно начала SASM-кода в байтах. </returns>
            public static int GetOffset(IEnumerable<Label> Labels, string Name)
            {
                foreach (Label l in Labels)
                {
                    if (l.Name == Name) { return l.Offset; }
                }
                return -1;
            }

            /// <summary>
            /// Метод, проверяющий, совпадает ли метка с данной. Метки считаются совпадающими, если они имеют одно и то же имя.
            /// </summary>
            /// <param name="obj"> Метка, которую требуется проверить на совпадение с текущей. </param>
            /// <returns> Булево значение, показывающее, совпадают ли метки. </returns>
            public override bool Equals(object obj)
            {
                if (!(obj is Label)) { return false; }
                return Name == (obj as Label).Name;
            }

            /// <summary>
            /// Метод, возвращающий хеш-код метки.
            /// </summary>
            /// <returns> Хеш-код метки. </returns>
            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
        }

        /// <summary>
        /// Словарь, содержащий загруженные внешние библиотеки. Ключами являются имена библиотек, а значениями - адреса их точек входа.
        /// </summary>
        public readonly static Dictionary<string, IntPtr> LoadedLibraries = new Dictionary<string, IntPtr>();
        /// <summary>
        /// Словарь, содержащий индексы начала переменных в массиве <see cref="Code"/>.
        /// Ключами являются имена переменных, а значениями - их индексы.
        /// </summary>
        public readonly Dictionary<string, int> VariableIndexes = new Dictionary<string, int>();
        /// <summary>
        /// Двоичное представление SASM-кода.
        /// </summary>
        public readonly byte[] Code;

        /// <summary>
        /// SASM-код пролога, сохраняющий состояние процессора.
        /// </summary>
        private static readonly string _sSASMProlog =
            @"
            push eax
            mov eax, [esp - 4h]
            mov [esp - 8h], eax
            mov eax, [esp - 18h]
            mov [esp - 0Ch], eax
            mov eax, [esp - 14h]
            mov [esp - 10h], eax
            pushfd
            xchg ebp, [esp]
            sub esp, 0Ch
            push ebp
            push ebx
            push ecx
            push edx
            push esi
            push edi
            mov ebp, esp
            ";
        /// <summary>
        /// SASM-код эпилога, восстанавливающий состояние процессора.
        /// </summary>
        private static readonly string _sSASMEpilog =
            @"
            mov esp, ebp
            pop edi
            pop esi
            pop edx
            pop ecx
            pop ebx
            pop ebp
            add esp, 0Ch
            xchg [esp], ebp
            popfd
            ret
            ";
        /// <summary>
        /// Словарь, содержащий внешние процедуры. Ключами являются имена процедур, а значениями - их адреса.
        /// </summary>
        private readonly Dictionary<string, IntPtr> _externs = new Dictionary<string, IntPtr>();
        /// <summary>
        /// Список всех меток, описанных в SASM-коде.
        /// </summary>
        private readonly LinkedList<Label> _labels = new LinkedList<Label>();
        /// <summary>
        /// Список меток, относящихся к переменным.
        /// </summary>
        private readonly LinkedList<Label> _variableLabels = new LinkedList<Label>();
        /// <summary>
        /// Словарь констант, доступных в текущий момент. Ключами являются имена констант, а значениями - их значения.
        /// </summary>
        private readonly Dictionary<string, string> _consts = new Dictionary<string, string>();
        /// <summary>
        /// Список строк SASM-кода после их частичной обработки.
        /// </summary>
        private readonly LinkedList<string> _lines;
        /// <summary>
        /// Массив инструкций, из которых состоит SASM-код.
        /// </summary>
        private readonly Instruction[] _instructions;
        /// <summary>
        /// Словарь начальных значений переменных. Ключами являются имена переменных, а значениями - байтовые представления их начальных значений.
        /// </summary>
        private readonly Dictionary<int, byte[]> _initialVarValues = new Dictionary<int, byte[]>();
        /// <summary>
        /// Список имён констант, которые были объявлены, а потом удалены. Содержит аргументы и локальные переменные процедур.
        /// </summary>
        private readonly HashSet<string> _removedConsts = new HashSet<string>();

        /// <summary>
        /// Событие, возникающее после подсчёта смещений в байтах всех меток.
        /// </summary>
        private event Action _labelOffsetsComputed;

        /// <summary>
        /// Оператор, преобразовывающий объект <see cref="SASMCode"/> в его двоичный код (возвращает свойство <see cref="SASMCode.Code"/>).
        /// </summary>
        /// <param name="obj"> Объект <see cref="SASMCode"/>, чей двоичный код требуется получить. </param>
        /// <seealso cref="Code"/>
        public static explicit operator byte[] (SASMCode obj) { return obj.Code; }

        /// <summary>
        /// Конструктор, создающий объект <see cref="SASMCode"/> из текстового SASM-кода. <br/>
        /// Эта перегрузка позволяет задать пользовательскую функцию для загрузки внешних библиотек.
        /// </summary>
        /// <param name="AsmCode"> Текстовый SASM-код. </param>
        /// <param name="LibraryLoader"> 
        /// Функция, используемая для загрузки внешних библиотек. Должна принимать имя библиотеки и возвращать адрес её точки входа. 
        /// </param>
        /// <param name="DebugFlag"> Флаг, демонстрирующий, нужно ли в отладочных целях в начало SASM-кода добавлять инструкцию int3. </param>
        /// <param name="AddProlog"> 
        /// Флаг, демонстрирующий, нужно ли в начало SASM-кода добавлять пролог <see cref="_sSASMProlog"/>, сохраняющий состояние процессора.
        /// </param>
        /// <seealso cref="SASMCode(string, bool, bool)"/>
        public SASMCode(string AsmCode, Func<string, IntPtr> LibraryLoader, bool DebugFlag = false, bool AddProlog = true)
        {
            if (AddProlog)
            {
                AsmCode = _sSASMProlog + AsmCode;
                _consts.Add("$first", "[ebp + 18h]");
                _consts.Add("$second", "[ebp + 1Ch]");
                _consts.Add("$this", "[ebp + 20h]");
                _consts.Add("$return", "[ebp + 28h]");
            }
            if (DebugFlag) { AsmCode = "int3" + Environment.NewLine + AsmCode; }
            _lines = new LinkedList<string>(AsmCode.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries));

            // Приводим всё к нижнему регистру.
            // Удаляем коментарии.
            // Обрабатываем и удаляем объявления констант.
            // Заменяем asmret на эпилог.
            #region First wave.
            _lines = new LinkedList<string>(_lines.Select((string s) =>
            {
                s = s.Replace('\'', '"');
                while (s.Contains("  ")) { s = s.Replace("  ", " "); }
                if (!s.Contains(" lib ") && !s.Contains("\"")) { s = s.ToLower(); }
                else if (s.Contains("\""))
                {
                    string[] tmp = s.Split('"');
                    for (int i = 0; i < tmp.Length; i += 2)
                    {
                        tmp[i] = tmp[i].ToLower();
                    }
                    s = string.Join("\"", tmp);
                }
                int index = s.IndexOf(';');
                if (index != -1) { s = s.Substring(0, index); }
                s = s.Trim();
                return s;
            }));
            _lines.DoAndRemove((string s) => s.Contains(" equ "), (string s) =>
            {
                string[] tmp = s.Split(' ');
                try
                {
                    _consts.Add(tmp[0], tmp[2]);
                }
                catch (ArgumentException)
                {
                    throw new SASMTranslationException($"Constant with name \"{tmp[0]}\" was already defined!", s);
                }
            });
            IEnumerable<string> epilogLines = _sSASMEpilog.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            epilogLines = epilogLines.Select((string s) => s.Trim().ToLower());
            _lines.Replace("asmret", epilogLines);
            _lines.RemoveAll((string s) => { return s == ""; });
            #endregion

            // Обрабатываем и удаляем объявления внешних процедур.
            // Обрабатываем объявления переменных.
            // Обрабатываем объявления процедур и локальных переменных.
            // Разворачиваем макрос invoke. 
            #region Second wave.
            _lines.DoAndRemove((string s) => s.Contains(" lib "), (string s) =>
            {
                string[] tmp = s.Split(' ');
                if (tmp[0] != "extern") { throw new SASMTranslationException("Wrong \"extern ... lib ... \" syntax!", s); }
                IntPtr libPtr = LibraryLoader(tmp[3]);
                try
                {
                    _externs.Add(tmp[1].ToLower(), GetProcAddress(libPtr, tmp[1]));
                }
                catch (ArgumentException)
                {
                    throw new SASMTranslationException($"Extern function with name \"{tmp[1]}\" was already defined!", s);
                }
            });
            bool procFlag = false;
            int locStackShift = 0;
            string locVars = "";
            LinkedListNode<string> procPrologEnd = null;
            LinkedListNode<string> currentNode = _lines.First;
            while (currentNode != null)
            {
                string s = currentNode.Value;
                if (s.StartsWith("db ") || s.StartsWith("dw ") || s.StartsWith("dd ") ||
                    s.Contains(" db ") || s.Contains(" dw ") || s.Contains(" dd "))
                {
                    string storeComm = s;
                    if (s.Contains(" db ") || s.Contains(" dw ") || s.Contains(" dd "))
                    {
                        int index = s.IndexOf(' ');
                        string labelName = s.Substring(0, index);
                        storeComm = s.Substring(index + 1).Trim();
                        _lines.AddBefore(currentNode, labelName + ':');
                    }
                    int size = 0;
                    char chSize = storeComm[1];
                    switch (chSize)
                    {
                        case 'b':
                            size = 1;
                            break;
                        case 'w':
                            size = 2;
                            break;
                        case 'd':
                            size = 4;
                            break;
                    }
                    LinkedList<string> args = _getDArgs(storeComm.Substring(3), size);
                    currentNode = _lines.Replace(currentNode, args.Select((string ss) => $"store{chSize} {ss}"));
                }
                else if (s.StartsWith("proc "))
                {
                    procFlag = true;
                    string procNameAndArgs = s.Substring(5);
                    int index = procNameAndArgs.IndexOf(' ');
                    string procName = index == -1 ? procNameAndArgs : procNameAndArgs.Substring(0, index);
                    string[] procArgs = index == -1 ? new string[0] : procNameAndArgs.Substring(index + 1).Split(',');
                    string[] locConsts = new string[procArgs.Length];
                    int locCount = 0;
                    int argStackShift = 8;
                    foreach (string ss in procArgs)
                    {
                        string[] tmp = ss.Trim().Split(':');
                        locConsts[locCount++] = $"addconst {tmp[0]}, [ebp+{argStackShift}]";
                        switch (tmp[1])
                        {
                            case "dword":
                                argStackShift += 4;
                                break;
                            case "word":
                                argStackShift += 2;
                                break;
                            default:
                                throw new SASMTranslationException($"Incorrect declaration of argument size: \"{tmp[1]}\".", s);
                        }
                        locVars += (locVars == "" ? "" : ",") + tmp[0];
                    }
                    if (procArgs.Length != 0) { _lines.AddBefore(currentNode, locConsts); }
                    currentNode = procPrologEnd = _lines.Replace(currentNode, procName + ":", "push ebp", "mov ebp, esp");
                }
                else if (s.StartsWith("local "))
                {
                    string[] l_locVars = s.Substring(6).Split(',');
                    string[] locConsts = new string[l_locVars.Length];
                    int locCount = 0;
                    foreach (string ss in l_locVars)
                    {
                        string[] tmp = ss.Split(':');
                        switch (tmp[1])
                        {
                            case "dword":
                                locStackShift += 4;
                                break;
                            case "word":
                                locStackShift += 2;
                                break;
                            default:
                                throw new SASMTranslationException($"Incorrect declaration of local variable size: \"{tmp[1]}\".", s);
                        }
                        locConsts[locCount++] = $"addconst {tmp[0]}, [ebp-{locStackShift}]";
                        locVars += (locVars == "" ? "" : ",") + tmp[0];
                    }
                    currentNode = _lines.Replace(currentNode, locConsts);
                }
                else if (s == "endp")
                {
                    procFlag = false;
                    if (locStackShift != 0)
                    {
                        _lines.AddAfter(procPrologEnd, $"sub esp, {locStackShift}");
                        locStackShift = 0;
                    }
                    if (locVars != "") { currentNode = _lines.Replace(currentNode, "remconst " + locVars); }
                    else
                    {
                        LinkedListNode<string> tmp = currentNode.Previous;
                        _lines.Remove(currentNode);
                        currentNode = tmp;
                    }
                }
                else if ((s.StartsWith("ret ") || s.StartsWith("retn ") || s.StartsWith("retf ") ||
                         s == "ret" || s == "retn" || s == "retf") && procFlag)
                {
                    _lines.AddBefore(currentNode, "mov esp, ebp", "pop ebp");
                }
                else if (s.StartsWith("invoke "))
                {
                    string nameAndArgs = s.Substring(7);
                    int index = nameAndArgs.IndexOf(' ');
                    string name = index != -1 ? nameAndArgs.Substring(0, index) : nameAndArgs;
                    string[] args = index != -1 ? nameAndArgs.Substring(index + 1).Split(',') : new string[0];
                    LinkedListNode<string> tmp = _lines.AddAfter(currentNode, "call " + name.Trim());
                    _lines.Replace(currentNode, args.Select((string ss) => "push " + ss.Trim()).Reverse());
                    currentNode = tmp;
                }
                currentNode = currentNode.Next;
            }
            #endregion

            // Разворачиваем вызовы внешних процедур в косвенные вызовы через ecx.
            // Разворачиваем макрос addr в push и mov.
            #region Third wave.
            foreach (string s in new LinkedList<string>(_lines))
            {
                if (s.StartsWith("call "))
                {
                    string name = s.Substring(5);
                    if (_externs.ContainsKey(name))
                    {
                        _lines.Replace(s, "mov ecx, " + name, "call ecx");
                    }
                }
                else if (s.StartsWith("push addr "))
                {
                    string operand = s.Substring(10);
                    if (operand.StartsWith("[") && operand.EndsWith("]"))
                    {
                        string[] newValues =
                        {
                            "push ecx",
                            "lea ecx, " + operand,
                            "xchg [esp], ecx"
                        };
                        _lines.Replace(s, newValues);
                    }
                    else
                    {
                        string[] newValues =
                        {
                            "push ecx",
                            "mov ecx, " + operand,
                            "add ecx, $this",
                            "xchg [esp], ecx"
                        };
                        _lines.Replace(s, newValues);
                    }
                }
                else if (s.StartsWith("mov ") && (s.Contains(",addr ") || s.Contains(" addr ")))
                {
                    string[] tmp = s.Substring(4).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (tmp[0].Contains("addr")) { throw new SASMTranslationException("Wrong using modifer \"addr\" with command \"mov\"!", s); }
                    tmp[1] = tmp[1].Replace(",addr ", " ").Replace(" addr ", " ");
                    string operand = tmp[1];
                    bool bracket = false;
                    for (int i = tmp[1].Length - 1; i >= 0; i--)
                    {
                        if (tmp[1][i] == ']') { bracket = true; }
                        else if (tmp[1][i] == '[') { bracket = false; }
                        else if (tmp[1][i] == ' ' && !bracket)
                        {
                            operand = tmp[1].Substring(i + 1);
                            break;
                        }
                    }
                    if (operand.StartsWith("[") && operand.EndsWith("]"))
                    {
                        _lines.Replace(s, $"lea {tmp[0]}, {tmp[1]}");
                    }
                    else if (_externs.ContainsKey(operand))
                    {
                        _lines.Replace(s, $"mov {tmp[0]}, {tmp[1]}");
                    }
                    else
                    {
                        string[] newValues =
                        {
                            $"mov {tmp[0]}, {tmp[1]}",
                            $"add {tmp[0]}, $this"
                        };
                        _lines.Replace(s, newValues);
                    }
                }
            }
            #endregion

            // Считаем инструкции и обрабатываем метки.
            #region Fourth wave.
            int count = 0;
            foreach (string s in new LinkedList<string>(_lines))
            {
                if (!s.EndsWith(":"))
                {
                    count++;
                    continue;
                }
                else
                {
                    string name = s.Trim(':');
                    if (name.Contains('[') || name.Contains(']'))
                    {
                        throw new SASMTranslationException($"Label name mustn't contain \"[\" or \"]\"!", s);
                    }
                    if (name.All((char c) => "1234567890abcdefh".Contains(c)))
                    {
                        throw new SASMTranslationException($"Label name mustn't contain only hex digits!", s);
                    }
                    if (Enum.TryParse(name, out Registers _))
                    {
                        throw new SASMTranslationException($"Label name mustn't match with any register name!", s);
                    }
                    Label label = new Label(name, count);
                    if (_labels.Contains(label))
                    {
                        throw new SASMTranslationException($"Label with name \"{name}\" was already defined!", s);
                    }
                    _labels.AddLast(label);
                    string nextValue = _lines.Find(s)?.Next.Value;
                    _lines.Remove(s);
                    if (nextValue == null) { break; }
                    if (nextValue.StartsWith("storeb ") || nextValue.StartsWith("storew ") || nextValue.StartsWith("stored "))
                    {
                        _variableLabels.AddLast(label);
                    }
                }
            }
            #endregion

            // Обрабатываем инструкции.
            #region Fifth wave
            _instructions = new Instruction[_lines.Count];
            int counter = 0;
            foreach (string s in _lines)
            {
                _instructions[counter++] = new Instruction(this, s);
            }
            #endregion

            // Сохраняем начальные значения переменных.
            // Вычисляем байтовые смещения меток.
            // Заканчиваем обработку инструкций, вызывая callback-и, заменяющие символьные операнды на их значения.
            // Собираем двоичный код из кода инструкций.
            #region Sixth wave
            int c_off = 0;
            foreach (Instruction i in _instructions)
            {
                i.Offset = c_off;
                if (i.Command.StartsWith("store"))
                {
                    _initialVarValues.Add(c_off, i.ByteCode);
                }
                c_off += i.ByteCode.Length + i.InstrPrefixes.Count;
            }
            foreach (Label l in _labels)
            {
                l.Offset = _instructions[l.CommandOffset].Offset;
            }
            _labelOffsetsComputed?.Invoke();
            foreach (Label l in _variableLabels)
            {
                VariableIndexes.Add(l.Name, l.Offset);
            }
            int capacity = 0;
            foreach (Instruction i in _instructions) { capacity += i.ByteCode.Length + i.InstrPrefixes.Count; }
            Code = new byte[capacity];
            counter = 0;
            foreach (Instruction i in _instructions)
            {
                foreach (byte b in i.InstrPrefixes) { Code[counter++] = b; }
                foreach (byte b in i.ByteCode) { Code[counter++] = b; }
            }
            #endregion
        }

        /// <summary>
        /// Конструктор, создающий объект <see cref="SASMCode"/> из текстового SASM-кода. <br/>
        /// Эта перегрузка для загрузки внешних библиотек использует кеширование и функцию LoadLibrary из библиотеки kernel32.dll.
        /// </summary>
        /// <param name="AsmCode"> Текстовый SASM-код. </param>
        /// <param name="DebugFlag"> Флаг, демонстрирующий, нужно ли в отладочных целях в начало SASM-кода добавлять инструкцию int3. </param>
        /// <param name="AddProlog"> 
        /// Флаг, демонстрирующий, нужно ли в начало SASM-кода добавлять пролог <see cref="_sSASMProlog"/>, сохраняющий состояние процессора.
        /// </param>
        /// <seealso cref="SASMCode(string, Func{string, IntPtr}, bool, bool)"/>
        /// <seealso cref="_defaultLibraryLoader(string)"/>
        public SASMCode(string AsmCode, bool DebugFlag = false, bool AddProlog = true)
            : this(AsmCode, _defaultLibraryLoader, DebugFlag, AddProlog) { }

        /// <summary>
        /// Метод, позволяющий получить строковое представление двоичного кода данного объекта <see cref="SASMCode"/>.
        /// Возвращаемая строка имеет формат, удобный для копирования в C#-код в качестве содержимого байтового массива,
        /// а также опциональные комментарии, описывающие соответствие двоичного кода и текстовых инструкций.
        /// </summary>
        /// <param name="IsHex">
        /// Флаг, демонстрирующий, должны ли использоваться шестнадцатеричные значения байтов двоичного кода вместо десятичных. 
        /// </param>
        /// <param name="IsReadable">
        /// Флаг, демонстрирующий, необходимо ли добавлять комментарии, описывающие соответствие двоичного кода и текстовых инструкций.
        /// </param>
        /// <param name="IsLined"> Флаг, демонстрирующий, необходимо ли добавлять переносы строки между инструкциями. </param>
        /// <returns> Строковое представление двоичного кода данного объекта <see cref="SASMCode"/>. </returns>
        public string GetStringCode(bool IsHex = true, bool IsReadable = true, bool IsLined = true)
        {
            LinkedList<Label> labels = new LinkedList<Label>(_labels.OrderBy((Label l) => l.CommandOffset));
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            int counter = 0;
            int padding = _instructions.Max((Instruction i) =>
            {
                return (i.InstrPrefixes.Count + i.ByteCode.Length) * (IsHex ? 6 : 4) - 1;
            }) + 3;
            if (IsReadable)
            {
                foreach (KeyValuePair<string, string> p in _consts)
                {
                    builder.Append("// " + p.Key + " equ " + p.Value + Environment.NewLine);
                }
                builder.Append(Environment.NewLine);
            }
            foreach (string s in _lines)
            {
                if (IsReadable && labels.First?.Value.CommandOffset == counter)
                {
                    string label_string = "// " + labels.First.Value.Name + ':';
                    builder.Append(label_string.PadLeft(padding + label_string.Length));
                    if (IsLined) { builder.Append(Environment.NewLine); }
                    labels.RemoveFirst();
                }
                Instruction i = _instructions[counter++];
                string line = string.Join(", ", i.InstrPrefixes.Select((byte b) =>
                    {
                        if (IsHex) { return "0x" + Convert.ToString(b, 16); }
                        return Convert.ToString(b);
                    }));
                if (line != "") { line += ", "; }
                line += string.Join(", ", i.ByteCode.Select((byte b) =>
                {
                    if (IsHex) { return "0x" + Convert.ToString(b, 16); }
                    return Convert.ToString(b);
                }));
                if (line != "") { line += ","; }
                builder.Append(line.PadRight(padding));
                if (IsReadable)
                {
                    builder.Append("// " + s);
                }
                if (IsLined)
                {
                    builder.Append(Environment.NewLine);
                }
            }
            return builder.ToString().Trim(Environment.NewLine.ToCharArray()).Trim(' ', ',');
        }

        /// <summary>
        /// Метод, позволяющий получить значение однобайтовой переменной, объявленной в SASM-коде, по её имени.
        /// </summary>
        /// <param name="VariableName"> Имя однобайтовой переменной. </param>
        /// <returns> Значение переменной с именем <paramref name="VariableName"/>. </returns>
        /// <seealso cref="GetWORDVariable(string)"/>
        /// <seealso cref="GetDWORDVariable(string)"/>
        /// <seealso cref="GetWStringVariable(string)"/>
        /// <seealso cref="GetAStringVariable(string)"/>
        public byte GetBYTEVariable(string VariableName)
        {
            VariableName = VariableName.ToLower();
            if (!VariableIndexes.ContainsKey(VariableName))
            {
                throw new ArgumentException($"Variable {VariableName} wasn't found.");
            }
            return Code[VariableIndexes[VariableName]];
        }

        /// <summary>
        /// Метод, позволяющий получить значение двубайтовой переменной, объявленной в SASM-коде, по её имени.
        /// </summary>
        /// <param name="VariableName"> Имя двубайтовой переменной. </param>
        /// <returns> Значение переменной с именем <paramref name="VariableName"/>. </returns>
        /// <seealso cref="GetBYTEVariable(string)"/>
        /// <seealso cref="GetDWORDVariable(string)"/>
        /// <seealso cref="GetWStringVariable(string)"/>
        /// <seealso cref="GetAStringVariable(string)"/>
        public short GetWORDVariable(string VariableName)
        {
            VariableName = VariableName.ToLower();
            if (!VariableIndexes.ContainsKey(VariableName))
            {
                throw new ArgumentException($"Variable {VariableName} wasn't found.");
            }
            int index = VariableIndexes[VariableName];
            if (index + 1 >= Code.Length)
            {
                throw new ArgumentOutOfRangeException($"Variable {VariableName} can't be getted as WORD.");
            }
            return (short)(Code[index] << 8 + Code[index + 1]);
        }

        /// <summary>
        /// Метод, позволяющий получить значение четырёхбайтовой переменной, объявленной в SASM-коде, по её имени.
        /// </summary>
        /// <param name="VariableName"> Имя четырёхбайтовой переменной. </param>
        /// <returns> Значение переменной с именем <paramref name="VariableName"/>. </returns>
        /// <seealso cref="GetBYTEVariable(string)"/>
        /// <seealso cref="GetWORDVariable(string)"/>
        /// <seealso cref="GetWStringVariable(string)"/>
        /// <seealso cref="GetAStringVariable(string)"/>
        public int GetDWORDVariable(string VariableName)
        {
            VariableName = VariableName.ToLower();
            if (!VariableIndexes.ContainsKey(VariableName))
            {
                throw new ArgumentException($"Variable {VariableName} wasn't found.");
            }
            int index = VariableIndexes[VariableName];
            if (index + 3 >= Code.Length)
            {
                throw new ArgumentOutOfRangeException($"Variable {VariableName} can't be getted as DWORD.");
            }
            return (Code[index] << 24) | (Code[index + 1] << 16) | (Code[index + 2] << 8) | Code[index + 3];
        }

        /// <summary>
        /// Метод, позволяющий получить значение переменной, объявленной в SASM-коде и представляющей Unicode-строку, по её имени.
        /// </summary>
        /// <param name="VariableName"> Имя переменной, представляющей Unicode-строку. </param>
        /// <returns> Значение переменной с именем <paramref name="VariableName"/>. </returns>
        /// <seealso cref="GetBYTEVariable(string)"/>
        /// <seealso cref="GetWORDVariable(string)"/>
        /// <seealso cref="GetDWORDVariable(string)"/>
        /// <seealso cref="GetAStringVariable(string)"/>
        public string GetWStringVariable(string VariableName)
        {
            VariableName = VariableName.ToLower();
            if (!VariableIndexes.ContainsKey(VariableName))
            {
                throw new ArgumentException($"Variable {VariableName} wasn't found.");
            }
            int index = VariableIndexes[VariableName];
            int i = index;
            for (; i < Code.Length - 1 && (Code[i] != 0 || Code[i + 1] != 0); i++) { }
            return System.Text.Encoding.Unicode.GetString(Code, index, i - index + 1);
        }

        /// <summary>
        /// Метод, позволяющий получить значение переменной, объявленной в SASM-коде и представляющей ASCII-строку, по её имени.
        /// </summary>
        /// <param name="VariableName"> Имя переменной, представляющей ASCII-строку. </param>
        /// <returns> Значение переменной с именем <paramref name="VariableName"/>. </returns>
        /// <seealso cref="GetBYTEVariable(string)"/>
        /// <seealso cref="GetWORDVariable(string)"/>
        /// <seealso cref="GetDWORDVariable(string)"/>
        /// <seealso cref="GetWStringVariable(string)"/>
        public string GetAStringVariable(string VariableName)
        {
            VariableName = VariableName.ToLower();
            if (!VariableIndexes.ContainsKey(VariableName))
            {
                throw new ArgumentException($"Variable {VariableName} wasn't found.");
            }
            int index = VariableIndexes[VariableName];
            int i = index;
            for (; i < Code.Length && Code[i] != 0; i++) { }
            return System.Text.Encoding.ASCII.GetString(Code, index, i - index);
        }

        /// <summary>
        /// Метод, позволяющий восстановить начальные значения переменных, объявленных в SASM-коде.
        /// </summary>
        public void RestoreVariables()
        {
            foreach (KeyValuePair<int, byte[]> p in _initialVarValues)
            {
                for (int i = 0; i < p.Value.Length; i++)
                {
                    Code[p.Key + i] = p.Value[i];
                }
            }
        }

        /// <summary>
        /// Метод, загружающий внешнюю библиотеку с заданным именем в случае, если она не была загружена ранее, и возвращающий адрес её точки входа.
        /// </summary>
        /// <param name="Name"> Имя внешней библиотеки. </param>
        /// <returns> Адрес точки входа внешней библиотеки с именем <paramref name="Name"/>. </returns>
        private static IntPtr _defaultLibraryLoader(string Name)
        {
            IntPtr libPtr;
            if (LoadedLibraries.Keys.Contains(Name))
            {
                libPtr = LoadedLibraries[Name];
            }
            else
            {
                libPtr = LoadLibrary(Name);
                LoadedLibraries.Add(Name, libPtr);
            }
            return libPtr;
        }

        /// <summary>
        /// Возвращает байтовое представление переменной, значение которой задано с помощью db, dw или dd.
        /// Поддерживаются строковые переменные, а также повторения с помощью dup.
        /// </summary>
        /// <param name="strArgs"> Строка, задающая значение переменной. </param>
        /// <param name="size"> Размер каждого элемента переменной. Для db - 1, для dw - 2, для dd - 4. </param>
        /// <param name="rep"> 
        /// Количество повторений <paramref name="strArgs"/>. Используется в рекурсивных вызовах; при первом вызове как правило равен единице.
        /// </param>
        /// <returns> Массив байт, содержащий байтовое представление <paramref name="strArgs"/>. </returns>
        private static LinkedList<string> _getDArgs(string strArgs, int size, int rep = 1)
        {
            LinkedList<string> _args = new LinkedList<string>();
            bool quot = false, bracket = false;
            string singleVal = "";
            for (int j = 0; j < strArgs.Length; j++)
            {
                if (strArgs[j] == ',' && !quot && !bracket)
                {
                    _args.AddLast(singleVal);
                    singleVal = "";
                    continue;
                }
                if (strArgs[j] == '"') { quot = !quot; }
                if (strArgs[j] == '(' && !quot) { bracket = true; }
                if (strArgs[j] == ')' && !quot) { bracket = false; }
                singleVal += strArgs[j];
            }
            _args.AddLast(singleVal);
            LinkedList<string> result = new LinkedList<string>();
            foreach (string s in _args)
            {
                string _s = s.Trim();
                if (_s.Contains(" dup "))
                {
                    int _index = _s.IndexOf(' ');
                    int _count = _s.Substring(0, _index).GetInt();
                    string _vals = _s.Substring(_index + 6, _s.Length - (_index + 7));
                    result = new LinkedList<string>(result.Concat(_getDArgs(_vals, size, _count)));
                }
                else if (_s.StartsWith("\""))
                {
                    if (size == 1)
                    {
                        foreach (char c in _s.Trim('"'))
                        {
                            result.AddLast(unchecked((byte)c).ToString());
                        }
                    }
                    else if (size == 2)
                    {
                        foreach (char c in _s.Trim('"'))
                        {
                            result.AddLast(((int)c).ToString());
                        }
                    }
                    else   // TODO: Is it really nessesary? Who use "dd" for storing strings?!
                    {
                        _s = _s.Trim('"') + (_s.Length % 2 == 0 ? "" : "\0");
                        for (int j = 0; j < _s.Length; j += 2)
                        {
                            result.AddLast((_s[j + 1] << 16 | _s[j]).ToString());
                        }
                    }
                }
                else
                {
                    result.AddLast(_s);
                }
            }
            LinkedList<string> _ex = new LinkedList<string>(result);
            for (int i = 0; i < rep - 1; i++) { result = new LinkedList<string>(result.Concat(_ex)); }
            return result;
        }
    }

    /// <summary>
    /// Класс, предоставляющий методы расширения, используемые методами класса <see cref="SASMCode"/>.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// <para> Метод расширения, выполняющий заданные действия для каждого элемента последовательности, 
        /// удовлетворяющего заданному предикату, после чего удаляющий этот элемент из последовательности. </para>
        /// <para> Этот метод расширения меняет переданную последовательность. </para>
        /// </summary>
        /// <typeparam name="T"> Тип элементов последовательности. </typeparam>
        /// <param name="enumerable"> Последовательность, элементы которой требуется проверить на соответствие предикату. </param>
        /// <param name="Predicate"> Предикат, на соответствие которому требуется проверить элементы последовательности. </param>
        /// <param name="Act"> Действие, которое требуется совершить для каждого элемента последовательности, который соответствует предикату. </param>
        public static void DoAndRemove<T>(this LinkedList<T> enumerable, Func<T, bool> Predicate, Action<T> Act)
        {
            foreach (T t in enumerable.Where(Predicate)) { Act(t); }
            enumerable.RemoveAll(Predicate);
        }

        /// <summary>
        /// <para> Метод расширения, добавляющий в <see cref="LinkedList{T}"/> 
        /// один или несколько элементов перед заданным узлом <see cref="LinkedListNode{T}"/>.
        /// Возвращает узел, содержащий последнее добавленное значение. </para>
        /// <para> Этот метод расширения меняет переданную последовательность. </para>
        /// </summary>
        /// <typeparam name="T"> Тип элементов связанного списка. </typeparam>
        /// <param name="list"> Связанный список, в который требуется добавить элементы. </param>
        /// <param name="node"> Узел, перед которым требуется добавить элементы. </param>
        /// <param name="values"> Значения, которые требуется добавить перед узлом <paramref name="node"/>. </param>
        /// <returns>  Узел, содержащий последнее добавленное значение из <paramref name="values"/>. </returns>
        /// <seealso cref="AddAfter{T}(LinkedList{T}, LinkedListNode{T}, T[])"/>
        public static LinkedListNode<T> AddBefore<T>(this LinkedList<T> list, LinkedListNode<T> node, params T[] values)
        {
            LinkedListNode<T> result = node;
            foreach (T value in values)
            {
                result = list.AddBefore(node, value);
            }
            return result;
        }

        /// <summary>
        /// <para> Метод расширения, добавляющий в <see cref="LinkedList{T}"/> 
        /// один или несколько элементов после заданного узла <see cref="LinkedListNode{T}"/>.
        /// Возвращает узел, содержащий последнее добавленное значение. </para>
        /// <para> Этот метод расширения меняет переданную последовательность. </para>
        /// </summary>
        /// <typeparam name="T"> Тип элементов связанного списка. </typeparam>
        /// <param name="list"> Связанный список, в который требуется добавить элементы. </param>
        /// <param name="node"> Узел, после которого требуется добавить элементы. </param>
        /// <param name="values"> Значения, которые требуется добавить после узла <paramref name="node"/>. </param>
        /// <returns>  Узел, содержащий последнее добавленное значение из <paramref name="values"/>. </returns>
        /// <seealso cref="AddBefore{T}(LinkedList{T}, LinkedListNode{T}, T[])"/>
        public static LinkedListNode<T> AddAfter<T>(this LinkedList<T> list, LinkedListNode<T> node, params T[] values)
        {
            LinkedListNode<T> result = node;
            foreach (T value in values)
            {
                result = list.AddAfter(result, value);
            }
            return result;
        }

        /// <summary>
        /// <para> Метод расширения, заменяющий в <see cref="LinkedList{T}"/> заданный узел <see cref="LinkedListNode{T}"/>
        /// на узел, содержащий заданное значение, и возвращающий этот новый узел. </para>
        /// <para> Этот метод расширения меняет переданную последовательность. </para>
        /// </summary>
        /// <typeparam name="T"> Тип элементов связанного списка. </typeparam>
        /// <param name="list"> Связанный список, узел которого требуется заменить. </param>
        /// <param name="oldValue"> Узел, который требуется заменить. </param>
        /// <param name="newValue"> Новое значение, на которое требуется заменить узел <paramref name="oldValue"/>. </param>
        /// <returns> Добавленный узел, содержащий значение <paramref name="newValue"/>. </returns>
        /// <seealso cref="Replace{T}(LinkedList{T}, LinkedListNode{T}, T[])"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, LinkedListNode{T}, IEnumerable{T})"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, T, T)"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, T, T[])"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, T, IEnumerable{T})"/>
        public static LinkedListNode<T> Replace<T>(this LinkedList<T> list, LinkedListNode<T> oldValue, T newValue)
        {
            if (oldValue == null) { return null; }
            LinkedListNode<T> result = list.AddBefore(oldValue, newValue);
            list.Remove(oldValue);
            return result;
        }

        /// <summary>
        /// <para> Метод расширения, заменяющий в <see cref="LinkedList{T}"/> заданный узел <see cref="LinkedListNode{T}"/>
        /// на один или несколько узлов, содержащих заданные значения. Возвращает узел, содержащий последнее добавленное значение. </para>
        /// <para> Этот метод расширения меняет переданную последовательность. </para>
        /// </summary>
        /// <typeparam name="T"> Тип элементов связанного списка. </typeparam>
        /// <param name="list"> Связанный список, узел которого требуется заменить. </param>
        /// <param name="oldValue"> Узел, который требуется заменить. </param>
        /// <param name="newValues"> Новые значения, на которое требуется заменить узел <paramref name="oldValue"/>. </param>
        /// <returns> Узел, содержащий последнее добавленное значение из <paramref name="newValues"/>. </returns>
        /// <seealso cref="Replace{T}(LinkedList{T}, LinkedListNode{T}, T)"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, LinkedListNode{T}, IEnumerable{T})"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, T, T)"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, T, T[])"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, T, IEnumerable{T})"/>
        public static LinkedListNode<T> Replace<T>(this LinkedList<T> list, LinkedListNode<T> oldValue, params T[] newValues)
        {
            if (oldValue == null) { return null; }
            foreach (T t in newValues) { list.AddBefore(oldValue, t); }
            LinkedListNode<T> result = oldValue.Previous;
            list.Remove(oldValue);
            return result;
        }

        /// <summary>
        /// <para> Метод расширения, заменяющий в <see cref="LinkedList{T}"/> заданный узел <see cref="LinkedListNode{T}"/>
        /// на один или несколько узлов, содержащих заданные значения. Возвращает узел, содержащий последнее добавленное значение. </para>
        /// <para> Этот метод расширения меняет переданную последовательность. </para>
        /// </summary>
        /// <typeparam name="T"> Тип элементов связанного списка. </typeparam>
        /// <param name="list"> Связанный список, узел которого требуется заменить. </param>
        /// <param name="oldValue"> Узел, который требуется заменить. </param>
        /// <param name="newValues"> Новые значения, на которое требуется заменить узел <paramref name="oldValue"/>. </param>
        /// <returns> Узел, содержащий последнее добавленное значение из <paramref name="newValues"/>. </returns>
        /// <seealso cref="Replace{T}(LinkedList{T}, LinkedListNode{T}, T)"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, LinkedListNode{T}, T[])"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, T, T)"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, T, T[])"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, T, IEnumerable{T})"/>
        public static LinkedListNode<T> Replace<T>(this LinkedList<T> list, LinkedListNode<T> oldValue, IEnumerable<T> newValues)
        {
            if (oldValue == null) { return null; }
            foreach (T t in newValues) { list.AddBefore(oldValue, t); }
            LinkedListNode<T> result = oldValue.Previous;
            list.Remove(oldValue);
            return result;
        }

        /// <summary>
        /// <para> Метод расширения, заменяющий в <see cref="LinkedList{T}"/> узел, содержащий заданное значение,
        /// на узел, содержащий заданное новое значение, и возвращающий этот новый узел. </para>
        /// <para> Этот метод расширения меняет переданную последовательность. </para>
        /// </summary>
        /// <typeparam name="T"> Тип элементов связанного списка. </typeparam>
        /// <param name="list"> Связанный список, узел которого требуется заменить. </param>
        /// <param name="oldValue"> Значение, которое содержится в узле, который требуется заменить. </param>
        /// <param name="newValue"> Новое значение, на которое требуется заменить узел, содержащий <paramref name="oldValue"/>. </param>
        /// <returns> Добавленный узел, содержащий значение <paramref name="newValue"/>. </returns>
        /// <seealso cref="Replace{T}(LinkedList{T}, LinkedListNode{T}, T)"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, LinkedListNode{T}, T[])"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, LinkedListNode{T}, IEnumerable{T})"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, T, T[])"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, T, IEnumerable{T})"/>
        public static LinkedListNode<T> Replace<T>(this LinkedList<T> list, T oldValue, T newValue)
        {
            return list.Replace(list.Find(oldValue), newValue);
        }

        /// <summary>
        /// <para> Метод расширения, заменяющий в <see cref="LinkedList{T}"/> узел, содержащий заданное значение,
        /// на один или несколько узлов, содержащих заданные новые значения.
        /// Возвращает узел, содержащий последнее добавленное значение. </para>
        /// <para> Этот метод расширения меняет переданную последовательность. </para>
        /// </summary>
        /// <typeparam name="T"> Тип элементов связанного списка. </typeparam>
        /// <param name="list"> Связанный список, узел которого требуется заменить. </param>
        /// <param name="oldValue"> Значение, которое содержится в узле, который требуется заменить. </param>
        /// <param name="newValues"> Новые значения, на которые требуется заменить узел, содержащий <paramref name="oldValue"/>. </param>
        /// <returns> Узел, содержащий последнее добавленное значение из <paramref name="newValues"/>. </returns>
        /// <seealso cref="Replace{T}(LinkedList{T}, LinkedListNode{T}, T)"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, LinkedListNode{T}, T[])"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, LinkedListNode{T}, IEnumerable{T})"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, T, T)"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, T, IEnumerable{T})"/>
        public static LinkedListNode<T> Replace<T>(this LinkedList<T> list, T oldValue, params T[] newValues)
        {
            return list.Replace(list.Find(oldValue), newValues);
        }

        /// <summary>
        /// <para> Метод расширения, заменяющий в <see cref="LinkedList{T}"/> узел, содержащий заданное значение,
        /// на один или несколько узлов, содержащих заданные новые значения.
        /// Возвращает узел, содержащий последнее добавленное значение. </para>
        /// <para> Этот метод расширения меняет переданную последовательность. </para>
        /// </summary>
        /// <typeparam name="T"> Тип элементов связанного списка. </typeparam>
        /// <param name="list"> Связанный список, узел которого требуется заменить. </param>
        /// <param name="oldValue"> Значение, которое содержится в узле, который требуется заменить. </param>
        /// <param name="newValues"> Новые значения, на которые требуется заменить узел, содержащий <paramref name="oldValue"/>. </param>
        /// <returns> Узел, содержащий последнее добавленное значение из <paramref name="newValues"/>. </returns>
        /// <seealso cref="Replace{T}(LinkedList{T}, LinkedListNode{T}, T)"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, LinkedListNode{T}, T[])"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, LinkedListNode{T}, IEnumerable{T})"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, T, T)"/>
        /// <seealso cref="Replace{T}(LinkedList{T}, T, T[])"/>
        public static LinkedListNode<T> Replace<T>(this LinkedList<T> list, T oldValue, IEnumerable<T> newValues)
        {
            return list.Replace(list.Find(oldValue), newValues);
        }

        /// <summary>
        /// <para> Метод расширения, удаляющий из <see cref="LinkedList{T}"/> все элементы, удовлетворяющие заданному предикату. </para>
        /// <para> Этот метод расширения меняет переданную последовательность. </para>
        /// </summary>
        /// <typeparam name="T"> Тип элементов связанного списка. </typeparam>
        /// <param name="list"> Связанный список, из которого требуется удалить элементы. </param>
        /// <param name="Predicate"> Предикат, определяющий, нужно ли удалить данный элемент из списка. </param>
        public static void RemoveAll<T>(this LinkedList<T> list, Func<T, bool> Predicate)
        {
            foreach (T t in new LinkedList<T>(list))
            {
                if (Predicate(t)) { list.Remove(t); }
            }
        }

        /// <summary>
        /// <para> Метод расширения, формирующий массив из заданного первого элемента и элементов заданного массива. </para>
        /// <para> Этот метод расширения не меняет переданный массив. </para>
        /// </summary>
        /// <typeparam name="T"> Тип элементов массивов. </typeparam>
        /// <param name="element"> Первый элемент. </param>
        /// <param name="ToAdd"> Массив, элементы которого требуется добавить к <paramref name="element"/>. </param>
        /// <returns> 
        /// Новый массив, первым элементом которого является <paramref name="element"/>, 
        /// а последующие взяты из <paramref name="ToAdd"/> с сохранением порядка.
        /// </returns>
        /// <seealso cref="Add{T}(T[], T[])"/>
        /// <seealso cref="Add{T}(T[], T)"/>
        public static T[] Add<T>(this T element, T[] ToAdd)
        {
            T[] result = new T[ToAdd.Length + 1];
            result[0] = element;
            for (int i = 0; i < ToAdd.Length; i++)
            {
                result[i + 1] = ToAdd[i];
            }
            return result;
        }

        /// <summary>
        /// <para> Метод расширения, производящий конкатенацию массивов. </para>
        /// <para> Этот метод расширения не меняет переданные массивы. </para>
        /// </summary>
        /// <typeparam name="T"> Тип элементов массивов. </typeparam>
        /// <param name="arr"> Первый массив для конкатенации. </param>
        /// <param name="ToAdd"> Второй массив для конкатенации. </param>
        /// <returns> Новый массив, содержащий элементы <paramref name="arr"/> и <paramref name="ToAdd"/> с соблюдением их порядка. </returns>
        /// <seealso cref="Add{T}(T, T[])"/>
        /// <seealso cref="Add{T}(T[], T)"/>
        public static T[] Add<T>(this T[] arr, T[] ToAdd)
        {
            T[] result = new T[arr.Length + ToAdd.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                result[i] = arr[i];
            }
            for (int i = 0; i < ToAdd.Length; i++)
            {
                result[arr.Length + i] = ToAdd[i];
            }
            return result;
        }

        /// <summary>
        /// <para> Метод расширения, добавляющий к заданному массиву заданный последний элемент. </para>
        /// <para> Этот метод расширения не меняет переданный массив. </para>
        /// </summary>
        /// <typeparam name="T"> Тип элементов массивов. </typeparam>
        /// <param name="arr"> Массив, к которому требуется добавить последний элемент. </param>
        /// <param name="ToAdd"> Последний элемент, который требуется добавить к <paramref name="arr"/>. </param>
        /// <returns> 
        /// Новый массив, первые элементы которого взяты из <paramref name="ToAdd"/> 
        /// с сохранением их порядка, а последним является <paramref name="ToAdd"/>.
        /// </returns>
        /// <seealso cref="Add{T}(T, T[])"/>
        /// <seealso cref="Add{T}(T[], T[])"/>
        public static T[] Add<T>(this T[] arr, T ToAdd)
        {
            T[] result = new T[arr.Length + 1];
            for (int i = 0; i < arr.Length; i++)
            {
                result[i] = arr[i];
            }
            result[arr.Length] = ToAdd;
            return result;
        }

        /// <summary>
        /// Метод расширения, позволяющий получить из двоичного, десятичного или шестнадцатеричного строкового
        /// представления числа его числовое значение. Поддерживаются положительные и отрицательные числа,
        /// а основание системы счисления задаётся последним символом. <br/>
        /// <list type="bullet">
        /// <item>
        /// <term> Двоичная </term> :
        /// <description> b </description>
        /// </item><br/>
        /// <item>
        /// <term> Десятичная </term> :
        /// <description> d </description>
        /// </item><br/>
        /// <item>
        /// <term> Шестнадцатеричная </term> :
        /// <description> h </description>
        /// </item><br/>
        /// </list>
        /// </summary>
        /// <param name="str"> Строковое представление числа. </param>
        /// <returns> Числовое значение. </returns>
        public static int GetInt(this string str)
        {
            int sign = 1;
            int @base = 10;
            if (str.StartsWith("-"))
            {
                sign = -1;
                str = str.Substring(1);
            }
            if (str.EndsWith("h"))
            {
                @base = 16;
                str = str.Substring(0, str.Length - 1);
            }
            else if (str.EndsWith("b"))
            {
                @base = 2;
                str = str.Substring(0, str.Length - 1);
            }
            else if (str.EndsWith("d"))
            {
                @base = 10;
                str = str.Substring(0, str.Length - 1);
            }
            return sign * Convert.ToInt32(str, @base);
        }

        /// <summary>
        /// Метод расширения, проверяющий, является ли данный символ шестнадцатеричной цифрой.
        /// </summary>
        /// <param name="c"> Символ, который требуется проверить. </param>
        /// <returns> Булево значение, демонстрирующее, является ли <paramref name="c"/> шестнадцатеричной цифрой. </returns>
        public static bool IsHexDigit(this char c)
        {
            return (c >= 47 && c <= 57) || (c >= 97 && c <= 102);
        }
    }
}
