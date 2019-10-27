using System.Drawing;
using System.Linq;
using CNNVisualization.Objects;
using ml.AI;
using ml.AI.CNN;
using ml.AI.CNN.Layers;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using WindowHandler;

namespace CNNVisualization
{
    public class Visualizer : WindowHandler.WindowHandler
    {
        public ConvolutionalNeuralNetwork Network;
        public Picture[][] Textures;
        public Texture GlobalTexture;

        public const float DefaultScale = 2.5f;

        public Visualizer(Window window, ConvolutionalNeuralNetwork network) : base(window)
        {
            Network = network;
        }

        protected override void OnUpdate()
        {

        }

        protected override void OnStart()
        {
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);

            StringRenderer renderer;
            ResourceManager.PushRenderer(renderer = new StringRenderer(
                StringRenderer.FullCharSet,
                new Font("DejaVu Sans Mono", 12, FontStyle.Regular),
                Brushes.Black));

            var input = new Bitmap("img1.png");
            var inputVolume = new Volume(input);
            var output = Network.ForwardPass(inputVolume);

            var globalBitmap = new Bitmap(Window.Width, Window.Height);
            var ax = Window.Width / Network.Layers.Count;
            var dx = ax - ax / (float)Network.Layers.Count;
            var id = 0;
            Textures = new Picture[Network.Layers.Count][];
            for (var i = 0; i < Network.Layers.Count; i++)
            {
                Textures[i] = new Picture[Network.Layers[i].OutDepth];

                float scale;
                if (Network.Layers[i].OutSize.Width == 1 &&
                    Network.Layers[i].OutSize.Height == 1) scale = DefaultScale * 2;
                else scale = DefaultScale;

                var dy = Window.Height / 2.0f -
                         (Network.Layers[i].OutSize.Height * scale / 2) / Network.Layers[i].OutDepth;
                for (int j = 0; j < Network.Layers[i].OutDepth; j++)
                {
                    var bmp = Network.Layers[i].ToBitmap(j, Color.AntiqueWhite, Color.Black);
                    var tex = new Texture(bmp);

                    ResourceManager.PushTexture(id++, tex);
                    Textures[i][j] = new Picture(
                        new Vector2(
                            ax + dx * i,
                            (float)(dy + (j - Network.Layers[i].OutDepth / 2) * (Network.Layers[i].OutSize.Height * scale / 2 * 2.5))),
                        tex, new Vector2(scale, scale));

                    AddObject(Textures[i][j]);

                }
            }

            using (var gr = Graphics.FromImage(globalBitmap))
            {
                gr.FillRegion(new SolidBrush(Color.FromArgb(255, 94, 91,102)),
                    new Region(new Rectangle(0, 0, Window.Width, Window.Height)));
                gr.SmoothingMode =
                    System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var drawPen = new Pen(new SolidBrush(Color.FromArgb(255, 30, 25, 30)));

                for (var i = 0; i < Textures.Length; i++)
                {
                    var tex = Textures[i];
                    var prevTex = i != 0 ?Textures[i - 1] : null;

                    if (prevTex != null)
                    {
                        if (Network.Layers[i] is FullyConnectedLayer ||
                            Network.Layers[i] is ConvolutionalLayer ||
                            Network.Layers[i] is InputLayer ||
                            Network.Layers[i] is SoftmaxLayer)
                        {
                            for (var cur = 0; cur < tex.Length; cur++)
                            for (var prev = 0; prev < prevTex.Length; prev++)
                                gr.DrawLine(drawPen,
                                    new PointF(
                                        tex[cur].Position.X + Network.Layers[i].OutSize.Height * 1.25f,
                                        tex[cur].Position.Y + Network.Layers[i].OutSize.Width * 1.25f),
                                    new PointF(
                                        prevTex[prev].Position.X + Network.Layers[i - 1].OutSize.Height * 1.25f,
                                        prevTex[prev].Position.Y + Network.Layers[i - 1].OutSize.Width * 1.25f));
                        }
                        else
                        {
                            for (var cur = 0; cur < tex.Length; cur++)
                            {
                                gr.DrawLine(drawPen,
                                    new PointF(
                                        tex[cur].Position.X + Network.Layers[i].OutSize.Height * 1.25f,
                                        tex[cur].Position.Y + Network.Layers[i].OutSize.Width * 1.25f),
                                    new PointF(
                                        prevTex[cur].Position.X + Network.Layers[i - 1].OutSize.Height * 1.25f,
                                        prevTex[cur].Position.Y + Network.Layers[i - 1].OutSize.Width * 1.25f));
                            }
                        }
                    }
                }
            }

            InsertObject(0, new Picture(Vector2.Zero, GlobalTexture = new Texture(globalBitmap), Vector2.One));
            ResourceManager.PushTexture(id++, GlobalTexture);

            var infoRenderer = new InfoRenderer(renderer);
            id = 0;

            var sorted = output.WeightsRaw.Select(p => new {index = id++, value = p})
                .OrderByDescending(p => p.value).Take(3).ToList();

            infoRenderer.Guesses     = sorted.Select(p => p.index).ToArray();
            infoRenderer.GuessValues = sorted.Select(p => p.value).ToArray();
            AddObject(infoRenderer);
        }
    }
}