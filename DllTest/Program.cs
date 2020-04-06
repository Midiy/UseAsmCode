using System;
using UseAsmCode;

using static UseAsmCode.Invoker;

namespace DllTest
{
    class Program
    {
        static readonly string _stringCopy =
            @"
            ; $first - pointer to string
            ; $second - pointer to length

            mov ebx, $first
            add ebx, 8
            mov ecx, $second
            mov ecx, [ecx]
            inc ecx
            mov eax, addr V_String
            L_loop:
            dec ecx
            mov dx, [ebx + ecx*2]
            mov [eax + ecx*2], dx
            test ecx, ecx
            jnz L_loop
            asmret

            V_String db 28 dup (0)
            ";

        static readonly string _stringReturn =
            @"
            ; $first - pointer to string
            ; $second - pointer to length

            mov ebx, $first
            mov ecx, $second
            mov ecx, [ecx]
            add ecx, 5
            mov eax, addr V_String
            L_loop:
            dec ecx
            mov dx, [ebx + ecx*2]
            mov [eax + ecx*2], dx
            test ecx, ecx
            jnz L_loop
            asmret

            V_String db 36 dup (0)
            ";

        static readonly string _getAndReturnObject =
            @"
            mov eax, $first
            asmret
            ";

        static readonly string _fullVonNeumann =
            @"
            ; Constants
            STD_OUTPUT_HANDLE equ -0Bh
            STD_INPUT_HANDLE equ -0Ah
            'Y' equ 59h
            'y' equ 79h
            '0' equ 30h
            'x' equ 78h
            
            ; Extern functions
            extern GetStdHandle lib kernel32.dll
            extern WriteConsoleA lib kernel32.dll
            extern WriteConsoleW lib kernel32.dll
            extern ReadConsoleA lib kernel32.dll
            
            ; Main program
            L_prg:
            xor eax, eax
            db 0Fh, 0C7h, 0F0h, 90h
            push eax
            invoke P_NumToHexString eax, addr V_HexString
            mov edx, addr V_HexString
            add edx, 0Ah
            mov byte [edx], 0Dh
            inc edx
            mov byte [edx], 0Ah
            invoke GetStdHandle STD_OUTPUT_HANDLE
            mov edx, addr V_OutHndl
            mov [edx], eax
            push eax
            invoke WriteConsoleA eax, addr V_HexString, 0Ch, addr V_WriteCount, 0
            pop eax
            invoke WriteConsoleW eax, addr V_ContinueMsg, 13h, addr V_WriteCount, 0
            invoke GetStdHandle STD_INPUT_HANDLE
            mov edx, addr V_InHndl
            mov [edx], eax
            invoke ReadConsoleA eax, addr V_ContinueInput, 3, addr V_ReadCount, 0
            mov edx, addr V_ReadCount
            cmp [edx], 2
            je L_prg
            mov edx, addr V_ContinueInput
            cmp byte [edx], 'Y'
            je L_prg
            cmp byte [edx], 'y'
            je L_prg
            pop eax
            asmret
            
            ; Procedures
            proc P_NumToHexString Number:DWORD, Buffer:DWORD
            mov edi, Buffer
            mov byte [edi], '0'
            mov byte [edi + 1], 'x'
            mov byte [edi + 0Ah], 0
            mov eax, Number
            mov ebx, 8
            L_loop:
            dec ebx
            mov edx, eax
            lea ecx, [ebx * 4]
            shl edx, cl
            and edx, 0F0000000h
            shr edx, 28
            cmp edx, 0Ah
            jl L_NumToStr
            add edx, 7
            L_NumToStr:
            add edx, 30h
            mov byte [ebx + edi + 2], dl
            test ebx, ebx
            jnz L_loop
            ret 8
            endp
            
            ; Variables
            V_HexString db 0Dh dup (0)
            V_ContinueMsg dw 'Продолжить? (Y/n)', 0Dh, 0Ah, 0
            V_ContinueInput db 4 dup (0)
            V_OutHndl dd 0
            V_InHndl dd 0
            V_ReadCount dd 0
            V_WriteCount dd 0
            ";

        static readonly string _asmInsertionSort =
            @"
            mov esi, $first
            mov edi, $second
            mov edi, [edi]
            mov ecx, 1
            cmp edi, 1
            jle L_end
            L_loop1:
            mov eax, [esi + ecx*4]
            mov edx, ecx
            L_loop2:
            mov ebx, [esi + edx*4 - 4]
            cmp eax, ebx
            jg L_loop2end
            mov [esi + edx*4], ebx
            dec edx
            jnz L_loop2
            L_loop2end:
            mov [esi + edx*4], eax
            inc ecx
            cmp ecx, edi
            jne L_loop1
            L_end:
            asmret
            ";

        static string _str = "Hello, World!";
        static int[] _arr, _arr1, _arr2;

        static void Main(string[] args)
        {
            unsafe
            {
                InvokeAsm((void*)0, (void*)0, new SASMCode(
                    @"
                    STD_OUTPUT_HANDLE equ -0Bh

                    extern GetStdHandle lib kernel32.dll
                    extern WriteConsoleW lib kernel32.dll
                
                    invoke GetStdHandle STD_OUTPUT_HANDLE
                    mov edx, addr V_OutHndl
                    mov [edx], eax
                    invoke WriteConsoleW eax, addr V_HelloMsg, 0Eh, addr V_WriteCount, 0
                    asmret

                    V_HelloMsg dw 'Hello World!', 0Dh, 0Ah, 0
                    V_OutHndl dd 0
                    V_WriteCount dd 0
                    ").Code);
            }
            Console.WriteLine();


            Console.WriteLine($"Исходная строка: {_str}");
            int l_len = _str.Length;
            SASMCode c1 = new SASMCode(_stringCopy);
            SafeInvokeAsm(ref _str, ref l_len, c1.Code);
            Console.WriteLine($"Строка в переменной V_String SASM-кода: {c1.GetWStringVariable("V_String")}");
            Console.WriteLine();

            Console.WriteLine($"Исходная строка: {_str}");
            SASMCode c2 = new SASMCode(_stringReturn);
            string copy = SafeInvokeAsm<string, int, string>(ref _str, ref l_len, (byte[])c2);
            Console.WriteLine($"Строка, указатель на которую вернул ассемблерный код: {copy}");
            Console.WriteLine();

            Random rnd = new Random();
            Console.WriteLine($"Передаём объект типа {rnd.GetType()} с хеш-кодом {rnd.GetHashCode()}.");
            Random retRnd = SafeInvokeAsm<Random, Random, Random>(ref rnd, ref rnd, new SASMCode(_getAndReturnObject));
            Console.WriteLine($"Получили объект типа {retRnd.GetType()} с хеш-кодом {retRnd.GetHashCode()}.");
            Console.WriteLine("Проверка функциональности - 10 случайных целых из интервала [0; 5).");
            for (int i = 0; i < 10; i++)
            {
                Console.Write(retRnd.Next(0, 5) + " ");
            }
            Console.WriteLine();
            Console.WriteLine();

            DateTime n = DateTime.Now;
            SASMCode full = new SASMCode(_fullVonNeumann);
            Console.WriteLine($"Время трансляции: {DateTime.Now - n}.");
            unsafe
            {
                int selected = (int)InvokeAsm((void*)0, (void*)0, (byte[])full);
                Console.WriteLine($"Вы выбрали число {selected} (0x{Convert.ToString(selected, 16)}).");
            }
            Console.WriteLine();

            Console.Write("Введите размер массива для сортировки: ");
            int arraySize = Convert.ToInt32(Console.ReadLine());
            _arr = new int[arraySize];
            _arr1 = new int[arraySize];
            _arr2 = new int[arraySize];
            Random rand = new Random();
            for (int i = 0; i < arraySize; i++) { _arr[i] = rand.Next(); }
            Array.Copy(_arr, _arr1, arraySize);
            Array.Copy(_arr, _arr2, arraySize);
            DateTime start1 = DateTime.Now;
            SASMCode sort = new SASMCode(_asmInsertionSort);
            DateTime start2 = DateTime.Now;
            unsafe
            {
                fixed (int* arrPtr = _arr1)
                {
                    InvokeAsm(arrPtr, &arraySize, (byte[])sort);
                }
            }
            DateTime finish = DateTime.Now;
            Console.WriteLine($"Сортировка вставками на ассемблере заняла (в т.ч. время трансляции): {finish - start1} ({start2 - start1}).");
            start1 = DateTime.Now;
            for (int i = 1; i < _arr2.Length; i++)
            {
                int tmp = _arr2[i];
                int j = i - 1;
                for (; j >= 0; j--)
                {
                    if (_arr2[j] > tmp) { _arr2[j + 1] = _arr2[j]; }
                    else
                    {
                        break;
                    }
                }
                _arr2[j + 1] = tmp;
            }
            finish = DateTime.Now;
            Console.WriteLine($"Сортировка вставками на C# заняла: {finish - start1}.");
            Array.Sort(_arr);
            bool equal = true;
            for (int i = 0; i < arraySize; i++)
            {
                if (_arr[i] != _arr1[i] || _arr[i] != _arr2[i])
                {
                    Console.WriteLine($"Разные значения на позиции {i} (ожидаемое - ассемблер - C#): {_arr[i]} - {_arr1[i]} - {_arr2[i]}...");
                    equal = false;
                    break;
                }
            }
            if (equal) { Console.WriteLine("Массивы отсортированы корректно!"); }
            Console.ReadKey(true);
        }
    }
}
