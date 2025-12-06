using System.Text;
using UnityEngine;

namespace PhaseAgnostic_Tooling_Tests
{
    public class UlpTestModuleInterface : MonoBehaviour
    {
        [SerializeField]
        int m_AnchorValue = 1;

        [SerializeField]
        private int[] m_TestData = UlpTestModule.Anchor_Values;

        [ContextMenu("Generate Exploratory Output")]
        private void GenerateExploratoryOutput()
        {
            StringBuilder sB = new(4096);

            sB.AppendLine("[Single Precision Exploratory Data]");

            UlpTestModule.GenerateExploratoryFloatData(ref sB);

            string exploratoryFloatData = sB.ToString();

            sB.Clear();

            Debug.Log(exploratoryFloatData);

            sB.AppendLine("[Double Precision Exploratory Data]");

            UlpTestModule.GenerateExploratoryDoubleData(ref sB);

            string exploratoryDoubleData = sB.ToString();

            sB.Clear();

            Debug.Log(exploratoryDoubleData);
        }

        /// <summary>
        /// Generate output data anchored on the multiple representations of <see cref="m_AnchorValue"/> within the float encoding space.
        /// </summary>
        [ContextMenu("Gen. Foundational Float Comparison")]
        private void GenerateInitialFloatComparison()
        {
            StringBuilder sB = new(4096);

            UlpTestModule.GenAnchoredFloatData(m_AnchorValue, ref sB);

            string prima = sB.ToString();

            sB.Clear();

            Debug.Log(prima);
        }

        /// <summary>
        /// Generate output data anchored on the multiple representations of <see cref="m_AnchorValue"/> within the double encoding space.
        /// </summary>
        [ContextMenu("Gen. Foundational Double Comparison")]
        private void InitializeDoubleForPivotComparison()
        {
            StringBuilder sB = new(4096);

            UlpTestModule.GenerateFoundationalDoubleTestData(m_AnchorValue, ref sB);

            string prima = sB.ToString();

            sB.Clear();

            Debug.Log(prima);
        }

        /// <summary>
        /// Generate output data anchored on the multiple representations of <see cref="m_AnchorValue"/> within the float encoding space.
        /// <para/>
        /// Based on more complex comparison logic that undergoes a remapping step to account for possible sign differences
        /// that produce erroneous results within the distance value returned.
        /// </summary>
        [ContextMenu("Pivot Value Remap")]
        private void InitializeFloatRemappingForPivotComparison()
        {
            StringBuilder sB = new(4096);

            UlpTestModule.GenAnchoredFloatRemapData(m_AnchorValue, ref sB);

            string prima = sB.ToString();

            sB.Clear();

            Debug.Log(prima);
        }

        /// <summary>
        /// Generate output data anchored on the multiple representations of <see cref="m_AnchorValue"/> within the float encoding space.
        /// <para/>
        /// Interacts with our most complex remapping data structure. Captures the intermediate states of the branchless
        /// remapping process within a data structure and generate string data from that structure.
        /// </summary>
        [ContextMenu("Pivot Value Remap Via Fold")]
        private void InitializeRemappingViaFoldForPivotComparison()
        {
            StringBuilder sB = new(4096);

            UlpTestModule.GenAnchoredFloatRemapViaFoldData(m_AnchorValue, ref sB);

            string prima = sB.ToString();

            sB.Clear();

            UlpTestFileOutput.WriteTextToFile("pivot_value_remap_via_fold", prima);
        }


        /// <summary>
        /// Initialize operational validation of the two strategies used to remap float
        /// values during ulp-distance comparison between values with different signs.
        /// </summary>
        [ContextMenu("Run Automated Strategy Comparison")]
        private void AutomateStrategyComparison()
        {
            StringBuilder sB = new(4096);

            UlpTestModule.CompareAlternateRemappingStrategies(ref sB);

            Debug.Log("[Strategy Comparison Complete]");

            var resultAsString = sB.ToString();

            sB.Clear();

            UlpTestFileOutput.WriteTextToFile("strategy_comparison", resultAsString);
        }


        [ContextMenu("Generate Multiple Anchor Outputs")]
        private void GenMultiAnchorOutput()
        {
            var sB = new StringBuilder(8192);

            UlpTestModule.GenerateMultiAnchorDataOutput(m_TestData, "anchorOutput", ref sB);

            sB.Clear();

            sB = null;
        }
    }
}