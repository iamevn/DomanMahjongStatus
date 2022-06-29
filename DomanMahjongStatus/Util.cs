using FFXIVClientStructs.FFXIV.Component.GUI;
using Optional;
using Optional.Collections;
using Optional.Unsafe;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DomanMahjongStatus
{
    public static class Util
    {
        public unsafe class Pointer<T> where T : unmanaged
        {
            public T* Ptr { get; init; }
            public T Deref { get => *Ptr; }
            public static implicit operator T*(Pointer<T> p) => p.Ptr;

            private Pointer(T* rawPtr)
            {
                Ptr = rawPtr;
            }

            public static Option<Pointer<U>> MaybeFrom<U>(U* maybeNull) where U : unmanaged
            {
                if (maybeNull == null)
                    return Option.None<Pointer<U>>();
                else
                    return Option.Some(new Pointer<U>(maybeNull));
            }

            public Pointer<U> Cast<U>() where U : unmanaged
            {
                return new Pointer<U>((U*)Ptr);
            }

        }

        public unsafe static Option<Pointer<T>> MaybePtr<T>(T* maybeNull) where T : unmanaged
            => Pointer<T>.MaybeFrom(maybeNull);

        public static T[] Rest<T>(this T[] arr) => new ArraySegment<T>(arr).Slice(1).ToArray();

        public static Option<int> MaybeParseInt(this string s)
        {
            if (int.TryParse(s, out int i))
                return i.Some();
            else
                return Option.None<int>();
        }

        public static Option<TEnum> MaybeParseEnum<TEnum>(this string s, bool ignoreCase = false) where TEnum : struct
        {
            if (Enum.TryParse(s, ignoreCase, out TEnum value))
                return value.Some();
            else
                return Option.None<TEnum>();
        }

        public static TResult[] Map<T, TResult>(this T[] arr, Func<T, TResult> f)
            => new List<T>(arr).Map(f).ToArray();
        public static TResult[] Map<T, TResult>(this T[] arr, Func<T, int, TResult> f) 
            => new List<T>(arr).Map(f).ToArray();
        public static List<TResult> Map<T, TResult>(this List<T> lst, Func<T, TResult> f)
        {
            var result = new List<TResult>();
            foreach (T elem in lst)
            {
                result.Add(f(elem));
            }
            return result;
        }
        public static List<TResult> Map<T, TResult>(this List<T> lst, Func<T, int, TResult> f)
        {
            var result = new List<TResult>();
            for (int i = 0; i < lst.Count; i += 1)
            {
                result.Add(f(lst[i], i));
            }
            return result;
        }

        public static T[] Filter<T>(this T[] arr, Func<T, bool> f)
            => new List<T>(arr).Filter(f).ToArray();
        public static T[] Filter<T>(this T[] arr, Func<T, int, bool> f)
            => new List<T>(arr).Filter(f).ToArray();
        public static List<T> Filter<T>(this List<T> lst, Func<T, bool> f)
        {
            var result = new List<T>();
            foreach (T elem in lst)
            {
                if (f(elem))
                {
                    result.Add(elem);
                }
            }
            return result;
        }
        public static List<T> Filter<T>(this List<T> lst, Func<T, int, bool> f)
        {
            var result = new List<T>();
            for (int i = 0; i < lst.Count; i += 1)
            {
                if (f(lst[i], i))
                {
                    result.Add(lst[i]);
                }
            }
            return result;
        }
    }
}