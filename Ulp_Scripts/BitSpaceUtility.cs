

using System;
using System.Runtime.CompilerServices;

using Unity.Mathematics;

namespace PhaseAgnostic_Diagnostics_StateCapture_Validation
{
    public static class BitSpaceUtility
    {
        /// <summary>
        /// Used to visually represent floats binaries as strings.
        /// </summary>
        /// <remarks>
        /// Specifically formatted to represent the area of precision that intel guarantees on its fast-reciprocal intrinsic functions.
        /// </remarks>
        private const string FLOAT_STR_TEMP = "[x][xxxx_xxxx][xxx_xxxx_xxxx|xxxx_xxxx_xxxx]";

        private const string DOUBLE_STR_TEMP = "[x][xxx_xxxx_xxxx][xxxx_xxxx_xxxx_xxxx_xxxx_xxxx_xxxx_xxxx_xxxx_xxxx_xxxx_xxxx_xxxx]";

        private const int FLOAT_SIZE = 32;

        private const int DOUBLE_SIZE = 64;

        /// # This needs to be moved to a more generic module, this is not just for the instrinsic rcp_ps comparison logic. 
        /// Sort of? Given we format it via a template that specific represents floats?
        /// <summary>
        /// # This needs to be moved to a more generic module, this is not just for the instrinsic rcp_ps comparison logic.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public static string GenerateBinaryFloatRepresentation(float x)
        {
            /// I just didn't feel like 'b','r','e','a','k','i','n','g' it up.
            /// I don't think this allocates, but I did not confirm.
            /// (Up here I mean, we have to alloc at the end I think)

            var template = FLOAT_STR_TEMP.AsSpan();

            Span<char> span = stackalloc char[template.Length];

            template.TryCopyTo(span);

            uint fasi;

            unsafe
            {
                fasi = *(uint*)&x;
            }

            /// Do we want to invert this..? 
            /// 
            /// Right now we're transcribing the bits as if zero starts 
            /// on the right, which is counter-intuitive for binary reads.
            /// 
            Span<int> bits = stackalloc int[FLOAT_SIZE];

            for(int i = 0; i < FLOAT_SIZE; i+=1)
            {
                var b = (fasi >> i) & 1;

                bits[i] = (int)(b);
            }

            int index = FLOAT_SIZE - 1;

            for(int i = 0; i < span.Length; i+=1)
            {
                /// We add the bit to '0' because we actually
                /// need the numerical value of the char '0'
                /// which doesn't change if it's zero or is
                /// upped to.. is it 48?
                if (span[i] == 'x')
                    span[i] = (char)('0' + bits[index--]);

                if (index < 0)
                    break;
            }

            return span.ToString();
        }

        public static string GenerateBinaryDoubleRepresentation(double x)
        {
            /// I just didn't feel like 'b','r','e','a','k','i','n','g' it up.
            /// I don't think this allocates, but I did not confirm.
            /// (Up here I mean, we have to alloc at the end I think)

            var template = DOUBLE_STR_TEMP.AsSpan();

            Span<char> span = stackalloc char[template.Length];

            template.TryCopyTo(span);

            ulong dasi;

            unsafe
            {
                dasi = *(ulong*)&x;
            }

            /// Do we want to invert this..? 
            /// 
            /// Right now we're transcribing the bits as if zero starts 
            /// on the right, which is counter-intuitive for binary reads.

            Span<int> bits = stackalloc int[DOUBLE_SIZE];

            for (int i = 0; i < DOUBLE_SIZE; i += 1)
            {
                var b = (dasi >> i) & 1;

                bits[i] = (int)(b);
            }

            int index = DOUBLE_SIZE - 1;

            for (int i = 0; i < span.Length; i += 1)
            {
                /// We add the bit to '0' because we actually
                /// need the numerical value of the char '0'
                /// which doesn't change if it's zero or is
                /// upped to.. is it 48?
                if (span[i] == 'x')
                    span[i] = (char)('0' + bits[index--]);

                if (index < 0)
                    break;
            }

            return span.ToString();
        }

        /// <summary>
        /// Scans <paramref name="expected"/> and <paramref name="result"/> bits and returns the last matching bit index found.
        /// </summary>
        /// <remarks>
        /// Scans occur from left-to-right. The resulting bit value, however behaves, as if the bits represent a 32-element
        /// collection, starting from the right, at index zero.
        /// </remarks>
        /// <returns></returns>
        /// # This needs to be moved to a more generic module, this is not just for the instrinsic rcp_ps comparison logic.
        public static int CountMatchingBits(float result, float expected)
        {
            Span<int> r_bits = stackalloc int[32];
            Span<int> e_bits = stackalloc int[32];

            int e = 0, r = 0;

            unsafe
            {
                e = *(int*)&expected;

                r = *(int*)&result;
            }

            /// This is probably kind of pointless in the sense that we could just check them here instead of building a collection 
            /// and then iterating through that collection. However, It also leaves the ability to return the binary as Memory<t>
            /// or just the array itself, and I want to leave that door open. Maybe, either way. It's not a big deal, if performance
            /// is an issue, I don't think it'll be here. If we actually do come to care about this spot, it'll be easy to merge loops.
            /// 
            for(int i = 0; i < 32; i+=1)
            {
                int shift = 31 - i;

                e_bits[shift] = (e >> shift) & 1;

                r_bits[shift] = (r >> shift) & 1;
            }

            int lastMatchingBit = -1;

            for(int i = 31; i >= 0; i-=1)
            {
                if (e_bits[i] == r_bits[i])
                {
                    lastMatchingBit = i;
                }

                else { break; }
            }

            return 
                lastMatchingBit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool FloatMatchWithinTolerance(float x, float y, int bitTolerance)
        {
            int xBits;
            int yBits;

            unsafe
            {
                xBits = *(int*)&x;
                yBits = *(int*)&y;
            }

            var xyDelta = math.abs(xBits - yBits);

            return xyDelta <= bitTolerance;
        }

        internal static bool FloatMatchWithinTolerance(float x, float y, float z, int bitTolerance)
        {
            return
                FloatMatchWithinTolerance(x, y, bitTolerance) && FloatMatchWithinTolerance(y, z, bitTolerance);
        }

        /// Should this be in the Ulp module..?
        /// <summary>
        /// Compares three different floats to determine if it is within the provided tolerance.
        /// </summary>
        internal static bool FloatMatchThreeWithinTolerance(float x, float y, float z, int bitTolerance, out float val)
        {
            /// Note that these values may differ slightly and we can not use direct equality
            /// for all of them. Especially when comparing the results of vectorized operations
            /// to scalar operations. Even prior to deviation based on reciprocal approximations.

            //var xBits = BitConverter.SingleToInt32Bits(x);
            //var yBits = BitConverter.SingleToInt32Bits(y);
            //var zBits = BitConverter.SingleToInt32Bits(z);

            int xBits;
            int yBits;
            int zBits;

            /// I don't know. Technically undefined behavior because we could provide a value that
            /// exceeds the bit space of the type we're converting, but I suspect it's going to be
            /// a good bit faster than the bit converter. I need to look into its implementation.
            unsafe
            {
                xBits = *(int*)&x;
                yBits = *(int*)&y;
                zBits = *(int*)&z;
            }

            var xyDelta = math.abs(xBits - yBits);

            var yzDelta = math.abs(yBits - zBits);

            if (xyDelta <= bitTolerance && yzDelta <= bitTolerance)
            {
                /// We select the buffer accumulation value, given it's the
                /// initial source for our cumulative signal state. Not great,
                /// given we have no enforcement of what value is in 'x' and
                /// are relying soley on the fact that's the order in which
                /// we passed the values.

                val = x;

                return true;
            }

            else
            {
                val = float.NaN;

                return false;
            }
        }
    }
}
   