

using PhaseAgnostic_Tooling;
using System.Text;

namespace PhaseAgnostic_Tooling_Tests
{
    #region Data Structures

    public struct DoubleUlpTestSourceData
    {
        private int m_ValueIndexTracker;

        internal readonly DoubleSourceDataPacket[] Packets;

        public DoubleUlpTestSourceData(int collectionSize)
        {
            m_ValueIndexTracker = 0;

            Packets = new DoubleSourceDataPacket[collectionSize];
        }

        internal DoubleSourceDataPacket this[int index]
        {
            get
            {
                return Packets[index];
            }
        }

        internal void AddData(double d, int index, int src, ValueConversionType cType)
        {
            if (m_ValueIndexTracker != -1)
            {
                Packets[m_ValueIndexTracker] = new(d, index, src, cType);

                m_ValueIndexTracker += 1;
            }

            else
                throw new System.Exception("Unable to add data; structure has already been populated at instantiation.");
        }
    }

    public struct DoubleSourceDataPacket
    {
        internal readonly double DoubleData;

        internal readonly int IndexMapping;

        internal readonly int SourceValue;

        internal readonly ValueConversionType ConversionType;

        public DoubleSourceDataPacket(double d, int map, int src, ValueConversionType cType)
        {
            DoubleData = d;

            IndexMapping = map;

            SourceValue = src;

            ConversionType = cType;
        }
    }

    #endregion

    public static partial class UlpTestModule
    {
        internal static void GenerateExploratoryDoubleData(ref StringBuilder sB)
        {
            GenerateExploratoryDoubleData(Anchor_Values, ref sB);
        }

        internal static void GenerateExploratoryDoubleData(int[] anchorValues, ref StringBuilder sB)
        {
            GenerateStringFromSourceStructure(GenerateDoubleDataFromSource(anchorValues), ref sB);
        }

        internal static void GenerateFoundationalDoubleTestData(int x, ref StringBuilder sB)
        {
            var sourceData = GenerateDoubleDataFromArgs(x);

            /// Objective?
            /// We want to demonstrate the difference between the bitcast and the base10 cast.
            /// Specifically, for the value of one - giving us an anchor that communicates to
            /// us valuable information about the float (ieee-754) encoding. I find that this
            /// is the most important piece of information in understanding where we go from here.

            sB.AppendLine("[Foundational Encoding Data]");

            sB.AppendLine();

            GenerateStringFromSourceStructure(sourceData, ref sB);

            sB.AppendLine();

            GenerateBasicDistanceMatrixData(sourceData, ref sB);
        }

        internal static DoubleUlpTestSourceData GenerateDoubleDataFromSource(int[] src)
        {
            /// Intermediate data structure still might be better, i.e arg structure
            /// But it should be fine, we have semi-immutability and it wouldn't have
            /// changed anything. There's a structural issue that would not be solved
            /// by the alternative - it would require a different design for the struct.
            /// There's a few readonly data collections we can leverage, if need be.

            int srcCount = src.Length;

            int reqSize = srcCount * 4;

            var ulpTestSourceData = new DoubleUlpTestSourceData(reqSize);

            for (int i = 0; i < srcCount; i += 1)
            {
                int x = src[i]; /// Alias just for the sake address locality

                unsafe
                {
                    double d = *(double*)&x;

                    ulpTestSourceData.AddData(d, i, x, ValueConversionType.PositiveBitCast);
                    ulpTestSourceData.AddData(-d, i, x, ValueConversionType.NegativeBitCast);

                    ulpTestSourceData.AddData(x, i, x, ValueConversionType.PositiveBase10Cast);
                    ulpTestSourceData.AddData(-x, i, x, ValueConversionType.NegativeBase10Cast);
                }
            }

            return ulpTestSourceData;
        }

        internal static DoubleUlpTestSourceData GenerateDoubleDataFromArgs(params int[] args)
        {
            return GenerateDoubleDataFromSource(args);
        }

        internal static void GenerateBasicDistanceMatrixData(DoubleUlpTestSourceData src, ref StringBuilder sB)
        {
            int srcCount = src.Packets.Length;

            for (int i = 0; i < srcCount; i += 1)
            {
                var iPacket = src[i];

                for (int j = 0; j < srcCount; j += 1)
                {
                    /// Does i == j have diagnostic value here? Or are we just creating noise?
                    if (i == j)
                        continue;

                    var jPacket = src[j];

                    GenerateBasicComparisonOutputData(i * j, iPacket, jPacket, ref sB);
                }
            }
        }

        internal static long GetDistanceAsUlp(double lhs, double rhs)
        {
            long x, y;

            unsafe
            {
                x = *(long*)&lhs;
                y = *(long*)&rhs;
            }

            return Unity.Mathematics.math.abs(x - y);
        }

        public static void GenerateStringFromSourceStructure(DoubleUlpTestSourceData src, ref StringBuilder sB)
        {
            int count = src.Packets.Length;

            /// We should probably alias the individual collections. But meh. Performance doesn't exactly matter here.

            for (int i = 0; i < count; i += 1)
            {
                var packet = src.Packets[i];

                ConvertTestDataToString
                (
                    packet.DoubleData,
                    packet.IndexMapping,
                    packet.SourceValue,
                    packet.ConversionType,
                    i,
                    ref sB
                );
            }
        }

        private static void ConvertTestDataToString(double d, int map, int srcVal, ValueConversionType t, int i, ref StringBuilder sB)
        {
            sB.AppendLine($"[Test Data Output ({i})]");
            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);
            sB.AppendLine($"[Source Value Mapping]:  \t({srcVal}) @ [{map}]");
            sB.AppendLine($"[Value Conversion Type]: \t({t})");
            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);
            DataToStringModule.GenerateMultipleRepresentations(d, ref sB);
            sB.AppendLine();
        }

        private static void GenerateBasicComparisonOutputData(int i, DoubleSourceDataPacket x, DoubleSourceDataPacket y, ref StringBuilder sB)
        {
            sB.AppendLine($"[Value (Double) Comparison Preface And Results]");

            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);

            sB.AppendLine($"[Preface Data]");

            ConvertTestDataToString(x.DoubleData, x.IndexMapping, x.SourceValue, x.ConversionType, i, ref sB);
            ConvertTestDataToString(y.DoubleData, y.IndexMapping, y.SourceValue, y.ConversionType, i, ref sB);

            sB.AppendLine("[Distance Comparison Results]");

            sB.AppendLine($"[Comparing ({x.DoubleData} , {y.DoubleData})");

            long units = GetDistanceAsUlp(x.DoubleData, y.DoubleData);

            sB.AppendLine($"[Units Between]: ({units})");

            sB.AppendLine();

            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);
        }

    }
}