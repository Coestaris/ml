using System;
using System.Drawing;
using System.Threading;
using ml.AI;
using OpenTK;
using OpenTK.Graphics.ES11;
using XORCalculator.Objects;
using WindowHandler;
using WindowHandler.Controls;

namespace XORCalculator
{
    internal static class TextureIds
    {
        public const int Button = 0;
        public const int ButtonActive = 1;

        public const int Checkbox = 2;
        public const int CheckboxActive = 3;
        public const int CheckboxChecked = 4;
        public const int CheckboxCheckedActive = 5;
    }

    public class NNVisualizer : WindowHandler.WindowHandler
    {
        public NeuralNetwork Network;
        public Teacher Teacher;

        public Checkbox Checkbox1;
        public Checkbox Checkbox2;

        internal StringRenderer _neuronStringRenderer;
        internal StringRenderer _buttonStringRenderer;
        internal StringRenderer _textRenderer;

        private Neuron[][] _neurons;
        private Axon[][] _axons;
        private InfoRenderer _infoRenderer;

        public bool _working;

        private const int StepsPerFrame = 2;
        private const int ErrorResetFactor = 0;
        private const int StepsDelay = 0;

        private Action _resetFunc;

        public NNVisualizer(Window window, NeuralNetwork network, Teacher teacher, Action ResetFunc) : base(window)
        {
            _resetFunc = ResetFunc;
            Network = network;
            Teacher = teacher;
        }

        public override void OnUpdate()
        {
            if (_working)
            {
                for (var i = 0; i < StepsPerFrame; i++)
                {
                    Step();
                    Thread.Sleep(StepsDelay);
                }
            }
        }

        private void Step()
        {
            Teacher.TeachStep(Network);

            DisplayValues();

            if (ErrorResetFactor != 0 && _infoRenderer.Step % ErrorResetFactor == 0)
                Teacher.ResetError();

            _infoRenderer.Error = Teacher.Error;
            _infoRenderer.Step++;
        }

        private void Reset()
        {
            _working = false;

            Teacher.ResetError();
            _resetFunc();
            DisplayValues();

            _infoRenderer.Error = Teacher.Error;
            _infoRenderer.Step = 0;
        }

        private void DisplayValues()
        {
            for(var l = 0; l < Network.Layers.Count; l++)
            for (var n = 0; n < Network.Layers[l].Size; n++)
            {
                _neurons[l][n].Activation = (float) Network.Layers[l].Activations[n];
                _neurons[l][n].Bias = (float) Network.Layers[l].Biases[n];
            }

            for(var l = 0; l < Network.Layers.Count - 1; l++)
            {
                var layer = Network.Layers[l];
                var nextLayer = Network.Layers[l + 1];
                for (var n = 0; n < layer.Size; n++)
                {
                    for (var j = 0; j < nextLayer.Size; j++)
                        _axons[l][n * nextLayer.Size + j].Weight =
                            (float)Network.Layers[l].Weights[n * nextLayer.Size + j];
                }
            }
        }

        private void Manual()
        {
            if (!_working)
            {
                Network.ForwardPass(new double[] {Checkbox1.Checked ? 1 : 0, Checkbox2.Checked ? 1 : 0});
                DisplayValues();
            }
        }

        public override void OnStart()
        {
            ResourceManager.PushTexture(TextureIds.Button, "button.png");
            ResourceManager.PushTexture(TextureIds.ButtonActive, "buttonActive.png");

            ResourceManager.PushTexture(TextureIds.Checkbox, "checkBox.png");
            ResourceManager.PushTexture(TextureIds.CheckboxActive, "checkBoxActive.png");
            ResourceManager.PushTexture(TextureIds.CheckboxChecked, "checkBoxChecked.png");
            ResourceManager.PushTexture(TextureIds.CheckboxCheckedActive, "checkBoxCheckedActive.png");

            _neuronStringRenderer = new StringRenderer(
                StringRenderer.NumericCharSet,
                new Font("DejaVu Sans Mono", 12, FontStyle.Regular),
                Brushes.Black);

            _buttonStringRenderer = new StringRenderer(
                StringRenderer.FullCharSet,
                new Font("DejaVu Sans Mono", 16, FontStyle.Regular),
                Brushes.White);

            _textRenderer = new StringRenderer(
                StringRenderer.FullCharSet,
                new Font("DejaVu Sans Mono", 16),
                Brushes.White);


            _neurons = new Neuron[Network.Layers.Count][];
            _axons   = new Axon  [Network.Layers.Count][];

            var xStep = Window.Width / (float)(Network.Layers.Count + 1);
            var x = xStep / 2;

            xStep += xStep / (Network.Layers.Count - 1);

            var layerCount = 0;
            foreach (var layer in Network.Layers)
            {
                _neurons[layerCount] = new Neuron[layer.Size];
                if(layer.WeightsNudge != null)
                    _axons[layerCount] = new Axon[layer.WeightsNudge.Length];

                var yStep = Window.Height / (float) (layer.Size + 1);
                var y = yStep;

                for (var i = 0; i < layer.Size; i++)
                {
                    _neurons[layerCount][i] = new Neuron(30, new Vector2(x, y))
                    {
                        _renderer = _neuronStringRenderer
                    };
                    y += yStep;
                }

                x += xStep;
                layerCount++;
            }

            for(int l = 0; l < Network.Layers.Count - 1; l++)
            {
                var layer = Network.Layers[l];
                var nextLayer = Network.Layers[l + 1];

                for (var i = 0; i < layer.Size; i++)
                {
                    for (var j = 0; j < nextLayer.Size; j++)
                    {
                        _axons[l][i * nextLayer.Size + j] = new Axon(
                            _neurons[l][i].Position,
                            _neurons[l + 1][j].Position,
                            (float) layer.Weights[i * nextLayer.Size + j]
                        );
                        AddObject(_axons[l][i * nextLayer.Size + j]);
                    }
                }
            }

            foreach (var neurons in _neurons)
                foreach (var neuron in neurons)
                    AddObject(neuron);

            AddObject(
                new Button(
                    TextureIds.ButtonActive,
                    TextureIds.Button,
                    new Vector2(65, 30),
                () => _working = true,
                    _buttonStringRenderer,
                    "Start"));

            AddObject(
                new Button(
                    TextureIds.ButtonActive,
                    TextureIds.Button,
                    new Vector2(190, 30),
                    () => _working = false,
                    _buttonStringRenderer,
                    "Stop"));


            AddObject(
                new Button(
                    TextureIds.ButtonActive,
                    TextureIds.Button,
                    new Vector2(315, 30),
                    () => Reset(),
                    _buttonStringRenderer,
                    "Reset"));

            AddObject(
                new Button(
                    TextureIds.ButtonActive,
                    TextureIds.Button,
                    new Vector2(440, 30),
                    () => Step(),
                    _buttonStringRenderer,
                    "Step"));

            Checkbox1 = new Checkbox(
                "Input1",
                TextureIds.Checkbox, TextureIds.CheckboxActive,
                TextureIds.CheckboxChecked, TextureIds.CheckboxCheckedActive,
                new Vector2(20, Window.Height - 50),
                (b) => Manual(), _buttonStringRenderer);

            Checkbox2 = new Checkbox(
                "Input2",
                TextureIds.Checkbox, TextureIds.CheckboxActive,
                TextureIds.CheckboxChecked, TextureIds.CheckboxCheckedActive,
                new Vector2(20, Window.Height - 20),
                (b) => Manual(), _buttonStringRenderer);

            AddObject(Checkbox1);
            AddObject(Checkbox2);

            _infoRenderer = new InfoRenderer(_textRenderer, Vector2.One);
            AddObject(_infoRenderer);

            Reset();

            base.OnStart();
        }
    }
}