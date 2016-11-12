﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

namespace NeuralNetModel
{
    public abstract class ANet
    {
        public delegate void TrainingHook(int batchNumber, ANet self = null);
        public TrainingHook Hook;

        #region Properties
        public ALayer this[int n]
        {
            get
            {
                return Layers[n];
            }
            protected set
            {
                Layers[n] = value;
            }
        }
        public int Count
        {
            get { return Layers.Count; }
        }
        private CostFunction _costFunction;
        public CostFunction CostFunction
        {
            get
            {
                return _costFunction;
            }
            protected set
            {
                SetCostFunction(value);
                _costFunction = value;
            }
        }
        public double LearningRate
        {
            get;
            protected set;
        }
        public double LastCost
        {
            get;
            protected set;
        }
        public bool Abort;
        #endregion

        #region Protected Fields
        protected ICostFunction CostFunc;
        protected List<ALayer> Layers;
        #endregion

        #region Public
        internal void Add(ALayer layer, int? pos = null)
        {
            if (pos == null)
                Layers.Add(layer);
            else
                Layers.Insert((int)pos, layer);
        }

        internal int Remove(ALayer l)
        {
            int pos = Layers.IndexOf(l);
            Layers.RemoveAt(pos);
            return pos;
        }

        internal ALayer Remove(int pos)
        {
            ALayer l = this[pos];
            Layers.RemoveAt(pos);
            return l;
        }
        #endregion

        #region Overridable
        internal virtual void SetParameters(double? learningRate = null, CostFunction? costFunc = null)
        {
            if (costFunc != null)
            {
                CostFunction = (CostFunction)costFunc;
            }
            if (learningRate != null)
                LearningRate = (double)learningRate;
        }
        internal abstract void Learn(HashSet<TrainingData> trainingSet, int batchSize);
        internal abstract Vector<double> Process(Vector<double> input);
        /// <summary>
        /// This method tests the net against the training set.
        /// </summary>
        /// <param name="testSet">Set of training data for the neural net.</param>
        /// <returns>A measure of error on the test set</returns>
        internal abstract double Test(HashSet<TrainingData> testSet);
        #endregion

        #region Private
        private void SetCostFunction(CostFunction costFunc)
        {
            switch (costFunc)
            {
                case CostFunction.MeanSquare:
                    CostFunc = new MeanSquare();
                    break;
                case CostFunction.CrossEntropy:
                    CostFunc = new CrossEntropy();
                    break;
            }
        }
        #endregion
    }
}
