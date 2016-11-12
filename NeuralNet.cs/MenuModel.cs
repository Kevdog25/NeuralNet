﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NeuralNetModel
{
    public static class MenuModel 
    {
        public delegate void UpdateCallback(int x, params double[] c);
        public static ANet.TrainingHook Hooks;
        public static int TestSampleFreq = 1;
        public static int GenericSampleFreq = 1;

        public static ANet CurrentNet;
        public static ALayer CurrentLayer;

        static Dictionary<string, HashSet<TrainingData>> CachedTrainingSets = new Dictionary<string, HashSet<TrainingData>>();
        static Dictionary<string, HashSet<TrainingData>> CachedTestSets = new Dictionary<string, HashSet<TrainingData>>();
        static HashSet<TrainingData> SelectedTrain;
        static HashSet<TrainingData> SelectedTest;

        #region Instantiate Things
        // Nets
        public static void NewMLP()
        {
            CurrentNet = new MLP();
        }

        // Layers
        public static void SetLayer(ALayer layer = null)
        {
            CurrentLayer = layer ?? new Layer();
        }
        #endregion

        #region SetParameters
        public static void SetNetParam(double? learningRate = null, CostFunction? costFunc = null)
        {
            CurrentNet.SetParameters(learningRate, costFunc);
        }

        public static void SetLayerParam(int? inputDim = null, int? outputDim = null, ActivationFunction? actFunc = null, RegularizationMode? regMode = null)
        {
            Layer localLayer = CurrentLayer as Layer;
            localLayer?.SetParameters(inputDim, outputDim, actFunc, regMode);
        }
        #endregion

        #region LoadTrainingData
        public static bool LoadTrainingSet(string name, string fp)
        {
            HashSet<TrainingData> data;
            bool success = LoadSet(fp, out data);
            if (success) CachedTrainingSets.Add(name, data);
            return success;
        }

        public static bool LoadTestSet(string name, string fp)
        {
            HashSet<TrainingData> data;
            bool success = LoadSet(fp, out data);
            if (success) CachedTestSets.Add(name, data);
            return success;
        }

        private static bool LoadSet(string fp, out HashSet<TrainingData> data)
        {
            data = new HashSet<TrainingData>();
            bool success = true;
            int inputDim = -1;
            int outputDim = -1;
            try
            {
                using (StreamReader trainingIn = new StreamReader(fp))
                {
                    while (!trainingIn.EndOfStream)
                    {
                        string[] line = trainingIn.ReadLine().Split(';');
                        string[] inputPiece = line[0].Split(',');
                        string[] outputPiece = line[1].Split(',');

                        if ((inputDim != -1 && inputPiece.Length != inputDim) ||
                            (outputDim != -1 && outputPiece.Length != outputDim))
                            throw new TrainingDataException("Failed to load data set. Inconsistent lines found.");

                        TrainingData td = new TrainingData(inputPiece.Length, outputPiece.Length);
                        for (var i = 0; i < inputPiece.Length; i++)
                            td.Data[i] = double.Parse(inputPiece[i]);
                        for (var i = 0; i < outputPiece.Length; i++)
                            td.Response[i] = double.Parse(outputPiece[i]);
                        data.Add(td);
                    }
                }
            }
            catch (Exception ex) when (IsFileException(ex))
            {
                success = false;
            }
            return success;
        }

        private static bool IsFileException(Exception ex)
        {
            return ex is ArgumentException || ex is ArgumentNullException ||
                ex is FileNotFoundException || ex is DirectoryNotFoundException || ex is IOException;
        }

        public static void SelectTrain(string name)
        {
            try
            {
                SelectedTrain = CachedTrainingSets[name];
            }
            catch
            {
                throw new NNException($"Cannot select training set with name {name}");
            }
        }

        public static void SelectTest(string name)
        {
            try
            {
                SelectedTest = CachedTestSets[name];
            }
            catch
            {
                throw new NNException($"Cannot select testing set with name {name}");
            }
        }
        #endregion

        #region Logging and Hooks
        public static void AddWeightMonitor(UpdateCallback hook)
        {
            if (CurrentNet == null)
                return;
            if(!(CurrentNet.Hook?.GetInvocationList()?.Contains(hook) ?? false))
            {
                CurrentNet.Hook += (int x, ANet self) =>
                {
                    if(x % GenericSampleFreq == 0)
                    {
                        double[] w = new double[CurrentNet.Count];
                        for(var i = 0; i < CurrentNet.Count; i++)
                        {
                            w[i] = CurrentNet[i].WeightMagnitude();
                        }
                        hook(GenericSampleFreq, w);
                    }
                };
            }
        }

        public static void AddCostMonitor(UpdateCallback hook)
        {
            if (CurrentNet == null)
                return;
            if (!(CurrentNet.Hook?.GetInvocationList()?.Contains(hook) ?? false))
            {
                CurrentNet.Hook += (int x, ANet self) =>
                {
                    hook(1, self.LastCost);
                };
            }
        }

        public static void AddTestMonitor(UpdateCallback hook)
        {
            if (CurrentNet == null)
                return;
            if (!(CurrentNet.Hook?.GetInvocationList()?.Contains(hook) ?? false))
            {
                CurrentNet.Hook += (int x, ANet self) =>
                {
                    if(x % TestSampleFreq == 0)
                    {
                        hook(TestSampleFreq, self.Test(SelectedTest));
                    }
                };
            }
        }

        public static void RemoveHooks()
        {
            CurrentNet.Hook = null;
        }
        #endregion

        #region Train
        public static void TrainNet(int nEpochs, int batchSize)
        {
            if (CurrentNet == null)
                throw new NNException("Error: Network is undefined.\nPlease load or create a network.");
            for (var i = 0; i < nEpochs; i++)
            {
                CurrentNet.Learn(SelectedTrain, batchSize);
                double error = CurrentNet.Test(SelectedTrain);
            }
        }
        #endregion

        public static int NumberOfLayers()
        {
            return CurrentNet.Count;
        }

        public static void InsertLayer(int pos = -1)
        {
            if(CurrentLayer != null)
                CurrentNet.Add(CurrentLayer, pos);
        }

        public static List<ALayer> GetLayers()
        {
            List<ALayer> layers = new List<ALayer>();
            for (var i = 0; i < CurrentNet.Count; i++)
                layers.Add(CurrentNet[i]);

            return layers;
        }
    }
}
