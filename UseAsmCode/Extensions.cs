using System;
using System.Collections.Generic;
using System.Linq;

namespace UseAsmCode.Extensions
{
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
