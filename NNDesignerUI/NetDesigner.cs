﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NeuralNetModel;

namespace NNDesignerUI
{
    public partial class NetDesigner : Form
    {
        private Stack<TabPage> PageStack;

        public NetDesigner()
        {
            InitializeComponent();
            PageStack = new Stack<TabPage>();
        }

        #region Utility
        private void ClearPreviewPanel()
        {
            PPreview.Controls.Clear();
            PPreviewEdit.Controls.Clear();
        }

        private void SwitchToPage(TabPage next)
        {
            ClearPreviewPanel();
            PageStack.Push(hiddenTabControl1.SelectedTab);
            hiddenTabControl1.SelectedTab = next;
        }

        private void Back(TabPage until = null)
        {
            ClearPreviewPanel();
            TabPage prev = null;
            if (until == null)
                prev = PageStack.Pop();
            else
            {
                while (PageStack.Count > 0 && !PageStack.Peek().Equals(until))
                    PageStack.Pop();
                if (PageStack.Count > 0)
                    prev = PageStack.Pop();
            }

            if(prev != null)
                hiddenTabControl1.SelectedTab = prev;
        }

        private void ShowLayerSummary(int index)
        {
            ALayer layer = MenuController.CurrentNet[index];

            // Set up the edit button.
            Button edit = new Button();
            edit.Text = "Edit";
            PPreviewEdit.Controls.Add(edit);
            edit.MouseClick +=
                (object s, MouseEventArgs e) =>
            {
                MenuController.SetLayer(layer);
                SwitchToPage(LayerEditor);
            };
            edit.Dock = DockStyle.Fill;
            if (layer is Layer)
            {
                Layer locL = layer as Layer;

                PPreview.Controls.Add(new Label() { Text = $"Input Dimension: {locL.InputDimension}", AutoSize = true}, 0, 0);
                PPreview.Controls.Add(new Label() { Text = $"Output Dimension: {locL.OutputDimension}", AutoSize = true }, 1, 0);
                PPreview.Controls.Add(new Label() { Text = $"Activation Function: {locL.ActivationFunction.ToString()}", AutoSize = true }, 0, 1);
                PPreview.Controls.Add(new Label() { Text = $"Regularization Mode: {locL.RegMode.ToString()}", AutoSize = true }, 1, 1);
            }
        }
        #endregion

        #region Main Menu
        private void BNewNet_Click(object sender, EventArgs e)
        {
            SwitchToPage(PPickANet);
        }

        private void BTrainNet_Click(object sender, EventArgs e)
        {
            SwitchToPage(PTrainNet);
        }

        private void BEditNet_Click(object sender, EventArgs e)
        {
            if (MenuController.CurrentNet is MLP)
                SwitchToPage(EditMLP);
        }
        #endregion

        #region Net Type Selection
        private void BBuildMLP_Click(object sender, EventArgs e)
        {
            MenuController.NewMLP();
            SwitchToPage(EditMLP);
            NAddLayerPos.Value = MenuController.NumberOfLayers();
        }
        #endregion

        #region Edit MLP
        private void EditMLP_Enter(object sender, EventArgs e)
        {
            DCostFunctionMLP.DataSource = Enum.GetValues(typeof(CostFunction));
            // Set default options up.
            MLP net = MenuController.CurrentNet as MLP;
            TMLPLearningRate.Text = net.LearningRate.ToString();
            DCostFunctionMLP.SelectedItem = net.CostFunction.ToString();
            NAddLayerPos.Value = net.Count;

            // Populate Summary
            PNetSummary.Controls.Clear();

            List<ALayer> layers = MenuController.GetLayers();
            for (var i = 0; i < layers.Count; i++)
            {
                Button b = new Button();
                b.Margin = new Padding(0);
                b.Height = PNetSummary.Height;


                int index = i;
                b.MouseClick += 
                    (object s, MouseEventArgs ev) => 
                {
                    ClearPreviewPanel();
                    ShowLayerSummary(index);
                };

                ALayer l = layers[i];
                string buttonText = "";
                if(l is Layer)
                {
                    Layer locL = l as Layer;
                    buttonText += $"{locL.InputDimension}\n" + new string('-',5) + $"\n{locL.OutputDimension}";
                }

                b.Text = buttonText;
                b.Parent = PNetSummary;
            }
        }

        private void BAddLayer_Click(object sender, EventArgs e)
        {
            MenuController.SetLayer();
            int index = (int)NAddLayerPos.Value;
            if (index > 0)
                NLayerInputDim.Value = MenuController.CurrentNet[index - 1].OutputDimension;
            if (index < MenuController.CurrentNet.Count)
                NLayerOutputDim.Value = MenuController.CurrentNet[index].InputDimension;
            SwitchToPage(LayerEditor);
        }

        private void NAddLayerPos_ValueChanged(object sender, EventArgs e)
        {
            int val = (int)NAddLayerPos.Value;
            NAddLayerPos.Value = Math.Min(Math.Max(val, 0), MenuController.NumberOfLayers());
        }

        private void TMLPLearningRate_Validated(object sender, EventArgs e)
        {
            double res;
            if (!double.TryParse(TMLPLearningRate.Text, out res))
                TMLPLearningRate.Text = "";
        }

        private void BEditMLPDone_Click(object sender, EventArgs e)
        {
            MenuController.SetNetParam(
                learningRate: double.Parse(TMLPLearningRate.Text), 
                costFunc: (CostFunction)DCostFunctionMLP.SelectedItem
                );
            Back(PMainMenu);
        }
        #endregion

        #region Layer Editor
        private void LayerEditor_Enter(object sender, EventArgs e)
        {
            DActivationFunction.DataSource = Enum.GetValues(typeof(ActivationFunction));
            CBRegularizationMode.DataSource = Enum.GetValues(typeof(RegularizationMode));
        }

        private void NLayerOutputDim_ValueChanged(object sender, EventArgs e)
        {
            MenuController.SetLayerParam(outputDim: (int)NLayerOutputDim.Value);
        }

        private void NLayerInputDim_ValueChanged(object sender, EventArgs e)
        {
            MenuController.SetLayerParam(outputDim: (int)NLayerInputDim.Value);
        }
      
        private void CBRegularizationMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            MenuController.SetLayerParam(regMode: (RegularizationMode)CBRegularizationMode.SelectedItem);
        }

        private void CBRegularizationMode_Validated(object sender, EventArgs e)
        {
        }

        private void DActivationFunction_SelectedIndexChanged(object sender, EventArgs e)
        {
            MenuController.SetLayerParam(actFunc: (ActivationFunction)DActivationFunction.SelectedItem);
        }

        private void DActivationFunction_Validation(object sender, CancelEventArgs e)
        {
        }
        
        private void BDoneLayerEdit_Click(object sender, EventArgs e)
        {
            MenuController.SetLayerParam((int)NLayerInputDim.Value,
                (int)NLayerOutputDim.Value,
                (ActivationFunction)DActivationFunction.SelectedItem,
                (RegularizationMode)CBRegularizationMode.SelectedItem);
            MenuController.InsertLayer((int)NAddLayerPos.Value);
            Back();
        }
        #endregion

        #region Train Net
        private void OpenFileDialog_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void BLoadTestSet_Click(object sender, EventArgs e)
        {
            OpenFileDialog.ShowDialog();
            string name = OpenFileDialog.FileName.Split('\\').Last();
            MenuController.LoadTestSet(name, OpenFileDialog.FileName);
            LBLoadedTest.Items.Add(name);
        }

        private void BLoadTrainSet_Click(object sender, EventArgs e)
        {
            OpenFileDialog.ShowDialog();
            string name = OpenFileDialog.FileName.Split('\\').Last();
            MenuController.LoadTrainingSet(name, OpenFileDialog.FileName);
            LBLoadedTrain.Items.Add(name);
        }

        private void BStartLearn_Click(object sender, EventArgs e)
        {
            try
            {
                MenuController.AddEpochPP(EpochPP);
                MenuController.TrainNet((string)LBLoadedTrain.SelectedItem, (string)LBLoadedTest.SelectedItem, SNEpochs.Value, SBatchSize.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void EpochPP(params double[] vals)
        {
            MessageBox.Show($"Heres the error: {vals[0]}");
        }

        private void LBLoadedTest_SelectedIndexChanged(object sender, EventArgs e)
        {
            BStartLearn.Enabled = LBLoadedTest.SelectedItem != null && LBLoadedTrain.SelectedItem != null;
        }

        private void LBLoadedTrain_SelectedIndexChanged(object sender, EventArgs e)
        {
            BStartLearn.Enabled = LBLoadedTest.SelectedItem != null && LBLoadedTrain.SelectedItem != null;
        }

        private void BBack_Train_Click(object sender, EventArgs e)
        {
            Back();
        }
        #endregion

    }
}
