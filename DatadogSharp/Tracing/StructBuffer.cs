using System;

namespace DatadogSharp.Tracing
{
    internal struct StructBuffer<T>
    {
        T[] array;
        int index;

        // not allows default(T);
        public StructBuffer(int capacity)
        {
            array = new T[capacity];
            index = 0;
        }

        public void Add(ref T value)
        {
            if (array.Length == index)
            {
                var newSize = index * 2;
                Array.Resize(ref array, newSize);
            }

            array[index] = value;
            index++;
        }

        public void Clear()
        {
            // not clear array value.
            index = 0;
        }

        public T[] ToArray()
        {
            if (array.Length == index) return array;

            Array.Resize(ref array, index);
            return array;
        }
    }
}