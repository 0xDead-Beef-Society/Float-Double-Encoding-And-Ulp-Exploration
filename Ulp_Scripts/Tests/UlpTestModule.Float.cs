

using PhaseAgnostic_Tooling;
using System;
using System.Text;
using UnityEngine;

namespace PhaseAgnostic_Tooling_Tests
{
    #region Dumb Stuff

    /// What's left?
    /// - [X] Double's foundational (1) data (matrix)
    /// - [X] Float's foundational (1) data with remapping; matrix comparison
    /// - [ ] Double's foundational (1) data with remapping; matrix comparison
    /// 
    /// We're going to encapsulate the double and float support in two different files, but in the same class.
    /// Just because I think the belong together and the overloads are essentially seemless, but it's just easier
    /// to really absorb and process. I find it difficult to handle multiple functions with very similar names,
    /// which are highly abstracted, with multiple overloads and similar signatures and navigate the code itself.
    /// 
    /// I honestly can't believe how much code we ended up writing for what I assumed was going to be a very small module.
    /// 
    /// I can't remember exactly where we left of yesterday, but I'm also curious at to whether or not
    /// we want to run a matrix-like comparison against the anchor value collection as a whole?
    /// May as well, there's information to be extracted. It's just such dense information that when we
    /// get to the point where we've got four different permutations of each anchor value and we've got
    /// each permutation represented in multiple forms, and then the epi-data about the primary value,
    /// it becomes very cumbersome to process. I think that's why focusing on one value in multiple forms,
    /// is probably the best place to start.
    /// 
    /// (Output For Both Of These)
    ///     Also a function that generates these from our source data.
    /// [Maybe]
    /// Investigation or at least mention of the difference between .Net abs and unity's mathematics.abs?  
    /// 
    /// The uhh.. so be aware that when we engage in use of .net math.abs and we provide it -1f, and 1f, depending on the
    /// order, when we due the bitcast to an integer to get the units, we end up with essentially a value that looks like
    /// [-(int32.max / 2) - (int32.max / 2)] and we end up with int32.min which of course is int32.max + 1 (~x + 1) and thus
    /// .net or mathf will throw an exception. If we're not on unity's platform or don't have their math function, you can
    /// emulate it with literally two lines of code.
    /// 
    /// Interestingly enough, our fold operation could be used as a branchless abs function.
    /// 
    /// You can find it in the UlpUtility
    /// [MethodImpl(MethodImplOptions.AggressiveInlining)]
    /// public static int Abs(int x)
    /// {
    ///    int y = -x;
    ///
    ///    return
    ///        x > y ? x : y;
    /// }

    #endregion

    public enum ValueConversionType
    {
        NegativeBitCast,
        PositiveBitCast,
        NegativeBase10Cast,
        PositiveBase10Cast
    }

    #region Data Structures

    public struct FloatUlpTestSourceData
    {
        internal readonly FloatSourceDataPacket[] Packets;

        private int m_ValueIndexTracker;

        public FloatUlpTestSourceData(int collectionSize)
        {
            m_ValueIndexTracker = 0;

            Packets = new FloatSourceDataPacket[collectionSize];
        }

        internal FloatSourceDataPacket this[int index]
        {
            get
            {
                return Packets[index];
            }
        }

        internal void AddData(float f, int index, int src, ValueConversionType cType)
        {
            if (m_ValueIndexTracker != -1)
            {
                Packets[m_ValueIndexTracker++] = new(f, index, src, cType);
            }

            else
                throw new System.Exception("Unable to add data; structure has already been populated at instantiation.");
        }
    }

    public struct FloatSourceDataPacket
    {
        internal readonly float FloatData;

        internal readonly int IndexMapping;

        internal readonly int SourceValue;

        internal readonly ValueConversionType ConversionType;

        public FloatSourceDataPacket(float f, int map, int src, ValueConversionType cType)
        {
            FloatData = f;

            IndexMapping = map;

            SourceValue = src;

            ConversionType = cType;
        }
    }

    #endregion

    public static partial class UlpTestModule
    {
        internal static readonly int[] Anchor_Values = new int[]
        {
            0, 1, 4, (1 << 4) - 1, (1 << 8) - 1, (1 <<  12) - 1
        };

        /// <summary>
        /// The most basic and naive implementation of distance-as-ulp.
        /// </summary>
        internal static int GetDistanceAsUlp(float lhs, float rhs)
        {
            int x, y;

            unsafe
            {
                x = *(int*)&lhs;
                y = *(int*)&rhs;
            }

            return Unity.Mathematics.math.abs(x - y);
        }

        #region Core


        /// <summary>
        /// Transmutes a collection of integers into multiple float-type values based on different conversion strategies,
        /// meant to illucinate fundamental float structure and encoding logic.
        /// </summary>
        internal static FloatUlpTestSourceData GenerateFloatDataFromSource(int[] src)
        {
            /// Intermediate data structure still might be better, i.e arg structure
            /// But it should be fine, we have semi-immutability and it wouldn't have
            /// changed anything. There's a structural issue that would not be solved
            /// by the alternative - it would require a different design for the struct.
            /// There's a few readonly data collections we can leverage, if need be.

            int srcCount = src.Length;

            int reqSize = srcCount * 4;

            var ulpTestSourceData = new FloatUlpTestSourceData(reqSize);

            for (int i = 0; i < srcCount; i += 1)
            {
                int x = src[i]; /// Alias just for the sake address locality

                unsafe
                {
                    /// Honestly, at this point, we should probably just make the packet data a union,
                    /// given the structure we have now. It is important to remember however, that the
                    /// mapping value is an index and must be of int32 type.
                    
                    float f = *(float*)&x;

                    ulpTestSourceData.AddData(f, i, x, ValueConversionType.PositiveBitCast);
                    ulpTestSourceData.AddData(-f, i, x, ValueConversionType.NegativeBitCast);

                    ulpTestSourceData.AddData(x, i, x, ValueConversionType.PositiveBase10Cast);
                    ulpTestSourceData.AddData(-x, i, x, ValueConversionType.NegativeBase10Cast);
                }
            }

            return ulpTestSourceData;
        }
        
        /// <summary>
        /// Purely an abstraction of convenience, meant for dynamic surface area, when interacting with <see cref="GenerateFloatDataFromSource(int[])"/>.
        /// </summary>
        internal static FloatUlpTestSourceData GenerateFloatDataFromArgs(params int[] args)
        {
            return GenerateFloatDataFromSource(args);
        }

        /// <summary>
        /// Generates output, based on the <see cref="Anchor_Values"/>, that is meant to provide some surface-level insight
        /// into the way floats are encoded and give us a general direction for our next step.
        /// </summary>
        internal static void GenerateExploratoryFloatData(ref StringBuilder sB)
        {
            GenerateExploratoryFloatData(Anchor_Values, ref sB);
        }

        /// <summary>
        /// Provides surface area for exploratory data generation with non-constant source values.
        /// </summary>
        internal static void GenerateExploratoryFloatData(int[] sourceValues, ref StringBuilder sB)
        {
            GenerateStringFromSourceStructure(GenerateFloatDataFromSource(sourceValues), ref sB);
        }

        #endregion

        #region Validation Automata
        internal static void CompareAlternateRemappingStrategies(ref StringBuilder sB)
        {
            CompareAlternateRemappingStrategies(Anchor_Values, ref sB);
        }

        internal static void CompareAlternateRemappingStrategies(int[] anchorValues, ref StringBuilder sB)
        {
            var testData = GenerateFloatDataFromSource(anchorValues);

            var packets = testData.Packets;

            int count = testData.Packets.Length;

            for(int i = 0; i < count; i+=1)
            {
                var iPacket = packets[i];

                for(int j = 0; j < count; j+=1)
                {
                    var jPacket = packets[j];

                    GenerateAlternativeResultsAndCompare(iPacket, jPacket, ref sB);
                }
            }
        }

        internal static void GenerateAlternativeResultsAndCompare(FloatSourceDataPacket x, FloatSourceDataPacket y, ref StringBuilder sB)
        {
            var packet_Branched = new UlpUtility.FloatUlpComparisonPacket(x.FloatData, y.FloatData);

            var packet_Branchless = new UlpUtility.FloatUlpDistanceViaFold(x.FloatData, y.FloatData);

            var result_x = packet_Branched.Results.RemappedUnits;

            var result_y = packet_Branchless.DistanceInUnits;

            if(result_x != result_y)
            {
                throw new System.Exception
                    ($"Validation of branchless logic failed; Source Values ({x.FloatData}, {y.FloatData}); Results: ({result_x}, {result_y})");
            }

            else
            {
                sB.AppendLine($"[Operational Result Match For ({x.FloatData}, {y.FloatData})]");

                sB.AppendLine($"[Distance As Units]: ({result_x}, {result_y})");

                sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);

                sB.AppendLine();
            }
        }

        #endregion

        #region Single Value Forensics And Analysis
        /// These functions provide us the ability to generate extensive amounts of data from a single value.
        /// I find these functions to probably be the most valuable, second only to the initial output that
        /// actually gave us a direction to move forward in. The value here is due to the isolation of the
        /// source value, which reduces the noise we see when generating data for more values, and the increased
        /// depth on a single value. This could just be a personal choice, but these I find to be the most
        /// useful for building understanding and intuition. I think that the best direction to move in from here,
        /// is to leverage these functions and isolate one anchor value at a time, and provides each value
        /// its own output file. The amount of information there is to parse is excessive, even for single values.
        /// 
        /// The anchor[ing] terminology is pretty much ad-hoc and just based on the initial array of integers
        /// used to generate our 'anchoring' data which essentially means our starting point. Or perhaps what
        /// we pivot on, purely meant to give us a direction.

        internal static void GenerateMultiAnchorDataOutput(int[] sourceData, string baseFileName, ref StringBuilder sB)
        {
            /// This may belong in the monobehavior/inspector interface logic. I'm fairly sure it does.
            /// Definitely does, but we've got to move on. It was a good expierment, but we've been long past
            /// the point where we were developing in the name of improving understanding and are now just worrying
            /// about the correctness of the code and the best-practices for code that we're probably never going
            /// to worry about again. 

            int count = sourceData.Length;

            for(int i = 0; i < count; i+=1)
            {
                var src = sourceData[i];

                GenAnchoredFloatData(src, ref sB);

                var output = sB.ToString();

                sB.Clear();

                sB.AppendJoin('_', new object[] { i, baseFileName, src, src.ToString("X") });

                var fileName = sB.ToString();

                sB.Clear();

                UlpTestFileOutput.WriteTextToFile(fileName, output);
            }    
        }


        /// <summary>
        /// Generates a multi-dimensional float data representation from a single integer, and then
        /// cross-compares each one of these floats in a matrix-like structure and generates labeled
        /// string data supplied to the provided <see cref="StringBuilder"/>.
        /// <para/>
        /// It should be noted that this function relies on our <b>basic distance analysis<b/>.
        /// </summary>
        internal static void GenAnchoredFloatData(int x, ref StringBuilder sB)
        {
            var sourceData = GenerateFloatDataFromArgs(x);

            /// Objective?
            /// We want to demonstrate the difference between the bitcast and the base10 cast.
            /// Specifically, for the value of one - giving us an anchor that communicates to
            /// us valuable information about the float (ieee-754) encoding. I find that this
            /// is the most important piece of information in understanding where we go from here.

            sB.AppendLine("[Encoding Data]");

            sB.AppendLine();

            GenerateStringFromSourceStructure(sourceData, ref sB);

            sB.AppendLine();

            GenerateBasicDistanceMatrixData(sourceData, ref sB);
        }

        /// <summary>
        /// Generates a multi-dimensional float data representation from a single integer, and then
        /// cross-compares each one of these floats in a matrix-like structure and generates labeled
        /// string data supplied to the provided <see cref="StringBuilder"/>.
        /// <para/>
        /// The cross-comparison matrix generation relies on the more advanced distance-as-ulp logic.
        /// <para/>
        /// The cross-comparison logic will provide the distance we expect to be correct and what it would
        /// be without the remapping process for negative float values.
        /// </summary>
        internal static void GenAnchoredFloatRemapData(int x, ref StringBuilder sB)
        {
            var sourceData = GenerateFloatDataFromArgs(x);

            sB.AppendLine("[Foundational Encoding Data]");

            sB.AppendLine();

            GenerateStringFromSourceStructure(sourceData, ref sB);

            sB.AppendLine();

            GenerateMappedDistanceMatrixData(sourceData, ref sB);
        }


        /// <summary>
        /// Generates a multi-dimensional float data representation from a single integer, and then
        /// cross-compares each one of these floats in a matrix-like structure and generates labeled
        /// string data supplied to the provided <see cref="StringBuilder"/>.
        /// <para/>
        /// The cross-comparison matrix generation relies on a remapping process that produces a data
        /// structure meant to record each intermediate step, each transformation of the initial input.
        /// </summary>
        internal static void GenAnchoredFloatRemapViaFoldData(int x, ref StringBuilder sB)
        {
            var sourceData = GenerateFloatDataFromArgs(x);

            sB.AppendLine("[Foundational Encoding Data]");

            sB.AppendLine();

            GenerateStringFromSourceStructure(sourceData, ref sB);

            sB.AppendLine();

            GenerateFoldedDistanceMatrixData(sourceData, ref sB);
        }

        #endregion

        #region Matrix Comparisons

        /// The matrix comparison functions take in a source data structure and compares the intenral values against
        /// themselves and eachother, as we would espect to see is a cross-matrix operation. We essentially treat one
        /// collection as two dimensions and process them with a nested loop structure. I'm not sure on the diagnostic
        /// value of comparing the same value against itself, so I've excluded it for now. The reason for this being
        /// that we're sure it's going to add additional noise. So, without being sure of the value added, we'll
        /// omit it for now.

        /// <summary>
        /// Cross-comparison and distance acquisition of the internal packets within the <see cref="FloatUlpTestSourceData"/> provided.
        /// </summary>
        internal static void GenerateBasicDistanceMatrixData(FloatUlpTestSourceData src, ref StringBuilder sB)
        {
            int srcCount = src.Packets.Length;

            for(int i = 0; i < srcCount; i+=1)
            {
                var iPacket = src[i];

                for(int j = 0; j < srcCount; j+=1)
                {
                    /// Does i == j have diagnostic value here? Or are we just creating noise?
                    if (i == j)
                        continue;

                    var jPacket = src[j];

                    GenerateBasicComparisonOutputData(i * j, iPacket, jPacket, ref sB);
                }
            }
        }

        /// <summary>
        /// Undertakes a matrix-like processing structure and generates string data based on <see cref="UlpUtility.FloatUlpComparisonPacket"/>.
        /// <br/>
        /// This data structure corresponds to our branched-fold operator meant to compensate for sign differences when comparing the number of units
        /// between two values.
        /// </summary>
        /// <remarks>
        /// This data structure contains the results of the remapping, not just the number of units.
        /// <para/>
        /// This is meant for diagnostic purposes.
        /// </remarks>
        internal static void GenerateMappedDistanceMatrixData(FloatUlpTestSourceData src, ref StringBuilder sB)
        {
            int count = src.Packets.Length;

            for(int i = 0; i < count; i+=1)
            {
                var iPacket = src[i];

                for(int j = 0; j < count; j+=1)
                {
                    /// Does i == j have diagnostic value here? Or are we just creating noise?
                    if (i == j)
                        continue;

                    var jPacket = src[j];

                    GenerateMappedComparisonOutputData(i * j, iPacket, jPacket, ref sB);
                }
            }
        }

        internal static void GenerateFoldedDistanceMatrixData(FloatUlpTestSourceData src, ref StringBuilder sB)
        {
            int count = src.Packets.Length;

            for (int i = 0; i < count; i += 1)
            {
                var iPacket = src[i];

                for (int j = 0; j < count; j += 1)
                {
                    /// Does i == j have diagnostic value here? Or are we just creating noise?
                    if (i == j)
                        continue;

                    var jPacket = src[j];

                    GenerateFoldedComparisonOutputData(i * j, iPacket, jPacket, ref sB);
                }
            }
        }

        #endregion

        #region String Manipulation

        public static void GenerateStringFromSourceStructure(FloatUlpTestSourceData src, ref StringBuilder sB)
        {
            int count = src.Packets.Length;

            for (int i = 0; i < count; i += 1)
            {
                var packet = src.Packets[i];

                ConvertSourceDataToString
                (
                    packet.FloatData, 
                    packet.IndexMapping, 
                    packet.SourceValue, 
                    packet.ConversionType, 
                    i, 
                    ref sB
                );
            }
        }

        private static void ConvertSourceDataToString(float f, int map, int srcVal, ValueConversionType t, int i, ref StringBuilder sB)
        {
            sB.AppendLine($"[Test Data Output ({i})]");
            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);
            sB.AppendLine($"[Source Value Mapping]:  \t({srcVal}) @ [{map}]");
            sB.AppendLine($"[Value Conversion Type]: \t({t})");
            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);
            DataToStringModule.GenerateMultipleRepresentations(f, ref sB);
            sB.AppendLine();
        }

        private static void GenerateBasicComparisonOutputData(int i, FloatSourceDataPacket x, FloatSourceDataPacket y, ref StringBuilder sB)
        {
            sB.AppendLine($"[Value (Float) Comparison Preface And Results]");

            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);

            sB.AppendLine($"[Preface Data]");

            ConvertSourceDataToString(x.FloatData, x.IndexMapping, x.SourceValue, x.ConversionType, i, ref sB);
            ConvertSourceDataToString(y.FloatData, y.IndexMapping, y.SourceValue, y.ConversionType, i, ref sB);

            sB.AppendLine("[Distance Comparison Results]");

            sB.AppendLine($"[Comparing ({x.FloatData} , {y.FloatData})");

            int units = GetDistanceAsUlp(x.FloatData, y.FloatData);

            sB.AppendLine($"[Units Between]: ({units})");

            sB.AppendLine();

            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);
        }

        /// <summary>
        /// Generates a data structure that contains the intial state provided, the folded result, and the post-remapping unit count along side
        /// the unit count we would expect without undertaking the remapping process.
        /// </summary>
        private static void GenerateMappedComparisonOutputData(int i, FloatSourceDataPacket x, FloatSourceDataPacket y, ref StringBuilder sB)
        {
            sB.AppendLine($"[Value (float) Comparison Preface And Results]");

            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);

            sB.AppendLine($"[Preface Data]");

            ConvertSourceDataToString(x.FloatData, x.IndexMapping, x.SourceValue, x.ConversionType, i, ref sB);
            ConvertSourceDataToString(y.FloatData, y.IndexMapping, y.SourceValue, y.ConversionType, i, ref sB);

            sB.AppendLine("[Distance Comparison Results]");

            sB.AppendLine($"[Comparing ({x.FloatData} , {y.FloatData})");

            /// I'm kind of wondering if we want to use the source packets themselves, as we could extract more information?

            var packet = new UlpUtility.FloatUlpComparisonPacket(x.FloatData, y.FloatData);

            GenerateStringFromUlpPacket(packet, ref sB);

            sB.AppendLine();

            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);
        }

        private static void GenerateFoldedComparisonOutputData(int i, FloatSourceDataPacket x, FloatSourceDataPacket y, ref StringBuilder sB)
        {
            sB.AppendLine($"[Value (float) Comparison Preface And Results]");

            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);

            sB.AppendLine($"[Comparing ({x.FloatData} , {y.FloatData})]");

            /// I'm kind of wondering if we want to use the source packets themselves, as we could extract more information?

            var packet = new UlpUtility.FloatUlpDistanceViaFold(x.FloatData, y.FloatData);

            GenerateStringFromUlpPacket(packet, ref sB);

            sB.AppendLine();

            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);
        }

        /// <summary>
        /// Generates string data based on <see cref="UlpUtility.FloatUlpComparisonPacket"/> and populates the provided
        /// <see cref="StringBuilder"/> with structured string data.
        /// </summary>
        /// Not actually sure how I feel about this yet, because this isn't a primitive or a generic type or something we expect to
        /// exist within the .net library and/or environment and is highly specific to the ulp-test code. May need to be else where.
        /// I don't want to pollute the actual utility class, though.
        internal static void GenerateStringFromUlpPacket(UlpUtility.FloatUlpComparisonPacket packet, ref StringBuilder sB)
        {
            var results = packet.Results;

            sB.AppendLine("[Float Ulp Comparison Packet (Branched)]");
            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);
            sB.AppendLine($"[Lhs Initial State]: ({packet.Lhs})");
            sB.AppendLine($"[Rhs Initial State]: ({packet.Rhs})");
            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);
            sB.AppendLine($"[Value States After Remapping]~");
            sB.AppendLine($"[Folded Lhs As Int]: ({results.LhsAsInt})");
            sB.AppendLine($"[Folded Rhs As Int]: ({results.RhsAsInt})");
            sB.AppendLine($"[Folded Lhs As Float]: ({results.LhsAsFloat})");
            sB.AppendLine($"[Folded Rhs As Float]: ({results.RhsAsFloat})");
            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);
            sB.AppendLine($"[Results; With And Without Remapping]");
            sB.AppendLine();
            sB.AppendLine($"[Units (Remapped)]: ({results.RemappedUnits})");
            sB.AppendLine($"[Units (NonMapped)]: ({results.NonMappedUnits})");
        }

        /// <summary>
        /// Takes in a ulp data packet based on the branchless fold operation and populates the provided <see cref="StringBuilder"/> with structured string data.
        /// </summary>
        internal static void GenerateStringFromUlpPacket(UlpUtility.FloatUlpDistanceViaFold packet, ref StringBuilder sB)
        {
            sB.AppendLine("[Ulp Distance Packet Contents (Branchless)]");
            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);

            /// Lhs State
            sB.AppendLine($"#[Left-Hand-Side]");
            sB.AppendLine($"[Initial State]: ({packet.Lhs})");
            DataToStringModule.GenerateMultipleRepresentations(packet.Lhs, ref sB);
            sB.AppendLine();
            GenerateStringFromFoldedFloatStructure(packet.LhsFolded, ref sB);

            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);

            /// Rhs State
            sB.AppendLine($"#[Right-Hand-Side]");
            sB.AppendLine($"[Initial State]: ({packet.Rhs})");
            DataToStringModule.GenerateMultipleRepresentations(packet.Rhs, ref sB);
            sB.AppendLine();
            GenerateStringFromFoldedFloatStructure(packet.RhsFolded, ref sB);

            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);

            sB.AppendLine($"[Distance As Units]: ({packet.DistanceInUnits})");
            sB.AppendLine();
            DataToStringModule.GenerateContrastRepresentations(packet.LhsFolded.ResultAsFloat, packet.RhsFolded.ResultAsFloat, ref sB);
        }

        /// <summary>
        /// Takes in an individual fold operation data structure and populates the provided <see cref="StringBuilder"/> with structured string data.
        /// </summary>
        internal static void GenerateStringFromFoldedFloatStructure(UlpUtility.FloatFoldResults fold, ref StringBuilder sB)
        {
            sB.AppendLine("[Folded State And Intermediate Values]");
            sB.AppendLine($"[Input Value]:  \t({fold.SourceValue})");
            sB.AppendLine($"[Input As Int]: \t({fold.SourceAsInt})");
            sB.AppendLine(DataToStringModule.TEXT_SEPARATOR);

            sB.AppendLine($"[Mask Result]: ({fold.SignMask})");
            DataToStringModule.GenerateMultipleRepresentations(fold.SignMask, ref sB);
            sB.AppendLine($"[First Fold Result]: ({fold.FirstFold})");
            DataToStringModule.GenerateMultipleRepresentations(fold.FirstFold, ref sB);
            sB.AppendLine($"[First Fold Result]: ({fold.SecondFold})");
            DataToStringModule.GenerateMultipleRepresentations(fold.SecondFold, ref sB);
            sB.AppendLine($"[Final Remapping]: ({fold.ResultAsFloat})");
            DataToStringModule.GenerateMultipleRepresentations(fold.ResultAsFloat, ref sB);
        }

        #endregion
    }
}
