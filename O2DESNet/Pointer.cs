using System;
using System.Globalization;

namespace O2DESNet
{
    [Serializable]
    public struct Pointer
    {
        /// <summary>
        /// Empty Pointer
        /// </summary>
        public static Pointer Empty => new();

        // This epsilon tolerance number is used for checking
        // equality which consider 1d = 1.0000000000000001d
        private const double epsilon = 1E-15d;

        private readonly double x;
        private readonly double y;
        private readonly double angle;
        private readonly bool flipped;

        /// <summary>
        /// Gets the x.
        /// </summary>
        public double X => x;

        /// <summary>
        /// Gets the y.
        /// </summary>
        public double Y => y;

        /// <summary>
        /// Gets the angle.
        /// </summary>
        public double Angle => angle;

        /// <summary>
        /// Gets a value indicating whether this <see cref="Pointer"/> is flipped.
        /// </summary>
        /// <value>
        ///   <c>true</c> if flipped; otherwise, <c>false</c>.
        /// </value>
        public bool Flipped => flipped;

        /// <summary>
        /// Gets a value indicating whether this instance is empty.
        /// Zero tolerance (epsilon) value is 1E-15.
        /// </summary>
        public bool IsEmpty => this == Pointer.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pointer"/> struct.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="angle">The angle.</param>
        /// <param name="flipped">if set to <c>true</c> [flipped].</param>
        public Pointer(double x, double y, double angle, bool flipped)
        {
            this.x = x;
            this.y = y;
            this.angle = angle;
            this.flipped = flipped;
        }

        /// <summary>
        /// Super position of two pointer
        /// </summary>
        public static Pointer operator *(Pointer inner, Pointer outer)
        {
            var radius = outer.Angle / 180d * Math.PI;
            return new Pointer(
                x: inner.X * Math.Cos(radius) - inner.Y * Math.Sin(radius) + outer.X,
                y: inner.Y * Math.Cos(radius) + inner.X * Math.Sin(radius) + outer.Y,
                angle: (outer.Angle + inner.Angle) % 360d,
                flipped: outer.Flipped ^ inner.Flipped
            );
        }

        /// <summary>
        /// Gets the inner pointer
        /// </summary>
        public static Pointer operator /(Pointer product, Pointer outer)
        {
            return product
                * new Pointer(x: -outer.X, y: -outer.Y, angle: 0d, flipped: false)
                * new Pointer(x: 0d, y: 0d, angle: -outer.Angle, flipped: outer.Flipped);
        }

        /// <summary>
        /// Inner pointer not equal to Outer pointer.
        /// </summary>
        /// <param name="inner">The inner.</param>
        /// <param name="outer">The outer.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Pointer inner, Pointer outer)
        {
            return !inner.Equals(outer);
        }

        /// <summary>
        /// Inner pointer equal to Outer pointer.
        /// </summary>
        /// <param name="inner">The inner.</param>
        /// <param name="outer">The outer.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Pointer inner, Pointer outer)
        {
            return inner.Equals(outer);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is not Pointer) return false;
            Pointer comp = (Pointer)obj;
            return
                Math.Abs(comp.X - X) <= (Math.Max(Math.Abs(comp.X), Math.Abs(X)) * epsilon) &&
                Math.Abs(comp.Y - Y) <= (Math.Max(Math.Abs(comp.Y), Math.Abs(Y)) * epsilon) &&
                Math.Abs(comp.Angle - Angle) <= (Math.Max(Math.Abs(comp.Angle), Math.Abs(Angle)) * epsilon) &&
                comp.Flipped == Flipped &&
                comp.GetType().Equals(GetType());
        }

        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture,
                "{{X={0}, Y={1}, Angle={2}, Flipped={3}}}", x, y, angle, flipped);
        }
    }
}