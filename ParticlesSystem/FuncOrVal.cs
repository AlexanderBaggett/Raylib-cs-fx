namespace Raylib_cs_fx
{
    public struct FuncOrVal<T>
    {
        private readonly T? value;
        private readonly Func<T>? funcVal;

        public FuncOrVal(T value)
        {
            this.value = value;
        }
        public FuncOrVal(Func<T> func)
        {
            this.funcVal = func;
        }

        public static implicit operator FuncOrVal<T>(T value)
        {
            return new FuncOrVal<T>(value);
        }
        public static implicit operator FuncOrVal<T>(Func<T> func)
        {
            return new FuncOrVal<T>(func);
        }

        //Because there are only 2 constructors one of these must be not null
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public T Value => value ?? funcVal();
#pragma warning restore CS8602 // Dereference of a possibly null reference.

    }

    public struct FuncOrVal<P,T>
    {
        private readonly T? value;
        private readonly Func<P,T>? funcVal;

        public FuncOrVal(T value)
        {
            this.value = value;
        }
        public FuncOrVal(Func<P,T> func)
        {
            this.funcVal = func;
        }

        public static implicit operator FuncOrVal<P,T>(T value)
        {
            return new FuncOrVal<P,T>(value);
        }
        public static implicit operator FuncOrVal<P,T>(Func<P,T> func)
        {
            return new FuncOrVal<P, T>(func);
        }

        //Because there are only 2 constructors one of these must be not null
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public T Value (P param) => value ??  funcVal(param);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

    }
}
