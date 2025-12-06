

using PhaseAgnostic_Diagnostics_StateCapture_Validation;
using System.Text;

namespace PhaseAgnostic_Tooling
{
    public static class DataToStringModule
    {
        public const string TEXT_SEPARATOR = "---------------------";

        public static void GenerateMultipleRepresentations(float f, ref StringBuilder sB)
        {
            unsafe
            {
                int x = *(int*)&f;
                var hex = x.ToString("X");
                var binary = BitSpaceUtility.GenerateBinaryFloatRepresentation(f);

                sB.AppendLine("[Initial Type]: (float)");
                sB.AppendLine(TEXT_SEPARATOR);
                sB.AppendLine($"[As Int]:      \t({x})");
                sB.AppendLine($"[As Float]:    \t({f})");
                sB.AppendLine($"[Hexadecimal]: \t(0x{hex})");
                sB.AppendLine($"[As Binary]:   \t({binary})");
                sB.AppendLine(TEXT_SEPARATOR);
            }
        }

        public static void GenerateContrastRepresentations(float lhs, float rhs, ref StringBuilder sB)
        {
            unsafe
            {
                int x = *(int*)&lhs;
                int y = *(int*)&rhs;

                var xBase16 = x.ToString("X");
                var yBase16 = y.ToString("X");

                var xBinary = BitSpaceUtility.GenerateBinaryFloatRepresentation(lhs);
                var yBinary = BitSpaceUtility.GenerateBinaryFloatRepresentation(rhs);

                sB.AppendLine("[Initial Type]: (float)");
                sB.AppendLine(TEXT_SEPARATOR);


                sB.AppendLine($"[As Ints]:");
                sB.AppendLine($"({x}) ({y})");
                sB.AppendLine($"[As Floats]:");
                sB.AppendLine($"({lhs}) ({rhs})");
                sB.AppendLine($"[As Hexadecimal]:");
                sB.AppendLine($"(0x{xBase16}) (0x{yBase16})");
                sB.AppendLine();
                sB.AppendLine($"[Lhs As Binary]: ({xBinary})");
                sB.AppendLine($"[Rhs As Binary]: ({yBinary})");
                sB.AppendLine(TEXT_SEPARATOR);
            }
        }

        public static void GenerateMultipleRepresentations(double d, ref StringBuilder sB)
        {
            unsafe
            {
                long x = *(long*)&d;
                var hex = x.ToString("X");
                var binary = BitSpaceUtility.GenerateBinaryDoubleRepresentation(d);

                sB.AppendLine("[Initial Type]: (double)");
                sB.AppendLine(TEXT_SEPARATOR);
                sB.AppendLine($"[As Double]:   \t({d})");
                sB.AppendLine($"[As Int64]:    \t({x})");
                sB.AppendLine($"[Hexadecimal]: \t(0x{hex})");

                /// Double's Bit Space Representation Too Long For Single Line?
                sB.AppendLine($"[As Binary]:   \n({binary})");
                sB.AppendLine(TEXT_SEPARATOR);
            }
        }

        public static void GenerateMultipleRepresentations(int i, ref StringBuilder sB)
        {
            unsafe
            {
                float x = *(float*)&i;
                var hex = i.ToString("X");
                var binary = BitSpaceUtility.GenerateBinaryFloatRepresentation(x);

                sB.AppendLine("[Initial Type]: (int32)");
                sB.AppendLine(TEXT_SEPARATOR);
                sB.AppendLine($"[As Int]:      \t({i})");
                sB.AppendLine($"[As Float]:    \t({x})");
                sB.AppendLine($"[Hexadecimal]: \t(0x{hex})");
                sB.AppendLine($"[As Binary]:   \t({binary})");
                sB.AppendLine(TEXT_SEPARATOR);
            }
        }
    }
}