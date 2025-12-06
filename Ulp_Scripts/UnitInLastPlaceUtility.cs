

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Unity.Mathematics;

namespace PhaseAgnostic_Tooling
{
    /// We need a module that isolates the conversion to string.
    /// MultiRepresentationProjector is a possible candidate..?

    /// <summary>
    /// 
    /// </summary>
    public static class UlpUtility
    {
        #region Branched Operator Structures
        /// <summary>
        /// Data structure that contains the remapping results of the given floats, as well as the original state of the floats provided at instantiation.
        /// <para/>
        /// This is a container for our branched remapping operation results.
        /// </summary>
        internal struct FloatUlpComparisonPacket
        {
            internal readonly float Lhs, Rhs;

            internal readonly FloatUnitDistancePacket Results;

            public FloatUlpComparisonPacket(float lhs, float rhs)
            {
                Lhs = lhs;
                Rhs = rhs;

                Results = GetDistance(lhs, rhs);
            }

            internal static FloatUnitDistancePacket GetDistance(float x, float y)
            {
                int lhs, rhs;

                int nonMappedUnits = 0;

                unsafe
                {
                    int xi = *(int*)&x;

                    lhs = x < 0 ? -(xi + Int32.MinValue) : xi;

                    int yi = *(int*)&y;

                    rhs = y < 0 ? -(yi + Int32.MinValue) : yi;

                    nonMappedUnits = math.abs(xi - yi);
                }

                int units = math.abs(lhs - rhs);

                return new(lhs, rhs, units, nonMappedUnits);
            }
        }

        internal struct DoubleUlpComparisonPacket
        {
            internal readonly float Lhs, Rhs;

            internal readonly DoubleUnitDistancePacket Results;

            public DoubleUlpComparisonPacket(float lhs, float rhs)
            {
                Lhs = lhs;
                Rhs = rhs;

                Results = GetDistance(lhs, rhs);
            }

            internal static DoubleUnitDistancePacket GetDistance(double x, double y)
            {
                long lhs, rhs;

                long unitsWithRemapping = 0;

                unsafe
                {
                    long xi = *(long*)&x;

                    lhs = x < 0 ? -(long)(xi + Int64.MinValue) : xi;

                    long yi = *(long*)&y;

                    rhs = y < 0 ? -(long)(yi + Int64.MinValue) : yi;

                    unitsWithRemapping = math.abs(xi - yi);
                }

                long units = math.abs(lhs - rhs);

                return new DoubleUnitDistancePacket(lhs, rhs, units, unitsWithRemapping);
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct FloatUnitDistancePacket
        {
            [FieldOffset(0)]
            internal readonly int LhsAsInt;

            [FieldOffset(4)]
            internal readonly int RhsAsInt;

            [FieldOffset(0)]
            internal readonly float LhsAsFloat;

            [FieldOffset(4)]
            internal readonly float RhsAsFloat;

            [FieldOffset(8)]
            internal readonly int RemappedUnits;

            [FieldOffset(12)]
            internal readonly int NonMappedUnits;

            public FloatUnitDistancePacket(int lhs, int rhs, int remappedUnits, int nonMappedUnits)
            {
                /// Compensation for C# requirements and their impact on unions
                this = default;

                LhsAsInt = lhs;

                RhsAsInt = rhs;

                RemappedUnits = remappedUnits;

                NonMappedUnits = nonMappedUnits;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct DoubleUnitDistancePacket
        {
            [FieldOffset(0)]
            internal readonly long LhsAsInt;

            [FieldOffset(4)]
            internal readonly long RhsAsInt;

            [FieldOffset(0)]
            internal readonly double LhsAsDouble;

            [FieldOffset(4)]
            internal readonly double RhsAsDouble;

            [FieldOffset(8)]
            internal readonly long RemappedUnits;

            [FieldOffset(12)]
            internal readonly long NonMappedUnits;

            public DoubleUnitDistancePacket(long lhs, long rhs, long remappedUnits, long nonMappedUnits)
            {
                /// Compensation for C# requirements and their impact on unions
                this = default;

                LhsAsInt = lhs;

                RhsAsInt = rhs;

                RemappedUnits = remappedUnits;

                NonMappedUnits = nonMappedUnits;
            }
        }

        #endregion /// - Branched Operators

        #region Branchless Operator Structures

        /// <summary>
        /// Data structure that encapsulates the original state of the floats provided at instantiation as well as the remapping results within a substructure.
        /// <para/>
        /// Represents the branchless form of the fold and remapping operation.
        /// </summary>
        internal readonly struct FloatUlpDistanceViaFold
        {
            /// <summary>
            /// Initial source values.
            /// </summary>
            internal readonly float Lhs, Rhs;

            internal readonly FloatFoldResults LhsFolded, RhsFolded;

            internal readonly int DistanceInUnits;

            public FloatUlpDistanceViaFold(float lhs, float rhs)
            {
                Lhs = lhs;
                Rhs = rhs;

                LhsFolded = new FloatFoldResults(lhs);
                RhsFolded = new FloatFoldResults(rhs);

                DistanceInUnits = math.abs(LhsFolded.ResultAsInt - RhsFolded.ResultAsInt);
            }
        }

        internal readonly struct DoubleUlpDistancePacket
        {
            /// <summary>
            /// Initial source values.
            /// </summary>
            internal readonly double Lhs, Rhs;

            internal readonly DoubleFoldResults LhsFolded, RhsFolded;

            internal readonly long DistanceInUnits;

            public DoubleUlpDistancePacket(double lhs, double rhs)
            {
                Lhs = lhs;
                Rhs = rhs;

                LhsFolded = new DoubleFoldResults(lhs);
                RhsFolded = new DoubleFoldResults(rhs);

                DistanceInUnits = math.abs(LhsFolded.ResultAsInt - RhsFolded.ResultAsInt);
            }
        }

        /// <summary>
        /// Acts as a container for each transition value within our float-remapping process. A process meant to facilitate the
        /// capture of distance between two floats as units (ulp), regardless of their signage.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        internal struct FloatFoldResults
        {
            /// We really need a better name for these individual values that represent intermediate state.
            /// Because they do no match what is really going on here. The process we're undertaking is the
            /// remapping of the negative float values to produce the correct distances via bit folding.
            /// So the idea that one of these values is the fold, or one of them is the mapped value is dumb.
            /// FIX ME; Fold one and two is better, but not much
            /// <summary>
            /// We really need a better name for these individual values that represent intermediate state.
            /// </summary>
            private const int FIELD_INSTANCE_COUNT = 5;

            [FieldOffset(0)]
            internal readonly float SourceValue;

            [FieldOffset(0)]
            internal readonly int SourceAsInt;

            [FieldOffset(4)]
            internal readonly int SignMask;

            [FieldOffset(8)]
            internal readonly int FirstFold;

            [FieldOffset(12)]
            internal readonly int SecondFold;

            [FieldOffset(16)]
            internal readonly int ResultAsInt;

            [FieldOffset(16)]
            internal readonly float ResultAsFloat;

            public FloatFoldResults(float source)
            {
                /// Just negotiating with the archaic c# requirements we have to navigate with unity.
                this = default;

                /// Union-based; just a little better than the bit hacks required to do this bit-exact conversions.
                SourceValue = source;

                /// We want to make sure we're using the signed integer interpretation to leverage sign-propagating.
                SignMask = (SourceAsInt >> 31);

                /// Conditional inversion (~x)
                FirstFold = SourceAsInt ^ SignMask;

                /// Finalize two's compliment, conditionally
                SecondFold = FirstFold - SignMask;

                /// Bias our results to account for float-type domain bifurcation
                /// Without the sign mask, it ends up skewing the correct results for positive values.
                ResultAsInt = SecondFold | (int.MinValue & SignMask);
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct DoubleFoldResults
        {
            private const int FIELD_INSTANCE_COUNT = 5;

            [FieldOffset(0)]
            internal readonly double SourceValue;

            [FieldOffset(0)]
            internal readonly long SourceAsInt;

            [FieldOffset(4)]
            internal readonly long SignMask;

            [FieldOffset(8)]
            internal readonly long FoldedBits;

            [FieldOffset(12)]
            internal readonly long MappedBits;

            [FieldOffset(16)]
            internal readonly long ResultAsInt;

            [FieldOffset(16)]
            internal readonly double ResultAsDouble;

            public DoubleFoldResults(double source)
            {
                /// Just negotiating with the archaic c# requirements we have to navigate with unity.
                this = default;

                /// Union-based; just a little better than the bit hacks required to do this bit-exact conversions.
                SourceValue = source;

                /// We want to make sure we're using the signed integer interpretation to leverage sign-propagating.
                SignMask = (SourceAsInt >> 31);

                /// Conditional inversion (~x)
                FoldedBits = SourceAsInt ^ SignMask;

                /// Finalize two's compliment, conditionally
                MappedBits = FoldedBits - SignMask;

                /// Bias our results to account for float-type domain bifurcation
                /// Without the sign mask, it ends up skewing the correct results for positive values.
                ResultAsInt = MappedBits | (long.MinValue & SignMask);
            }
        }

        /// Note that at the time of writing this we have not confirm the logic is correct and we're building a scaffold
        /// for comparing it to the branched mechanism that we know works, so we can compare the binary results.
        /// (12/2/25)
        /// <summary>
        /// Generates a container that tracks each step in the folding process used to acquire the correct integer
        /// representation needed for comparing floats for distance as units.
        /// </summary>
        /// <remarks>
        /// Provides a <b>branchless</b> compensatory mechanism that allows us to compare the ULP distance between two floats,
        /// even if they do not have matching signs.
        /// </remarks>
        internal static FloatFoldResults GetFoldResults(float f)
        {
            return new(f);
        }

        internal static DoubleFoldResults GetFoldResults(double d)
        {
            return new DoubleFoldResults(d);
        }

        #endregion

        internal static float GetUlpMagnitude(float f)
        {
            unsafe
            {
                int x = *(int*)&f;

                if (x >= Int32.MaxValue)
                    throw new System.Exception($"Unable to acquire ulp magnitude for ({f}); conversion roll-over detected @ ({x.ToString("X")})");

                int y = x + 1;

                float next = *(float*)&y;

                return next - f;
            }
        }

        internal static double GetUlpMagnitude(double d)
        {
            unsafe
            {
                long x = *(long*)&d;

                if (x >= long.MaxValue)
                    throw new System.Exception($"Unable to acquire ulp magnitude for ({d}); conversion roll-over detected @ ({x.ToString("X")})");

                long y = x + 1;

                double next = *(double*)&y;

                return next - d;
            }
        }

        /// <summary>
        /// Query the distance in possible representations (ulp) between two floats.
        /// </summary>
        /// <remarks>
        /// Safe for values with different signs.
        /// </remarks>
        internal static int GetDistanceAsUnits(float x, float y)
        {
            return math.abs(ConvertAndFoldFloat(x) - ConvertAndFoldFloat(y));
        }

        internal static long GetDistanceAsUnits(double x, double y)
        {
            return math.abs(ConvertAndFoldDouble(x) - ConvertAndFoldDouble(y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static int ConvertAndFoldFloat(float f)
        {
            int x = *(int*)&f;

            /// Should probably make this a const
            int sMask = (x >> 31);

            /// (Two's compliment; Conditional Equivalent (~x + 1))
            int folded = (x ^ sMask) - sMask;

            return folded | (Int32.MinValue & sMask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe static long ConvertAndFoldDouble(double d)
        {
            long x = *(long*)&d;

            /// Should probably make this a const
            long sMask = (x >> 63);

            /// (Two's compliment; Conditional Equivalent (~x + 1))
            long folded = (x ^ sMask) - sMask;

            return folded | (Int64.MinValue & sMask);
        }

        /// A very simplistic absolute function that mimics the mathematics version.
        /// Just in case we need it outside of unity. The .net function will throw in
        /// places that we do not want it to (like abs(int32.min)).
        /// <summary>
        /// Basic absolute value function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Abs(int x)
        {
            int y = -x;

            return 
                x > y ? x : y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Abs(long x)
        {
            long y = -x;

            return 
                x > y ? x : y;
        }

        /// So I'm going to be guessing here, we need to actually come back and validate as I haven't tested this.
        /// The assumption I'm making is that the region/domain bifurcation that occurs for floats at int32.min is
        /// then going to happen for doubles at long.min - though I wouldn't bet the house on it, given we're not
        /// talking about double the mantissa bits. I'm not really sure. Maybe we should investigate first..?
    }
}