namespace GMaster.Views
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Core.Camera;
    using Core.Tools;
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.UI.Xaml;
    using Windows.Foundation;
    using Windows.UI;

    public class FrameRenderer : IDisposable
    {
        private readonly object frameDrawLock = new object();
        private readonly CanvasControl view;
        private FrameData currentFrame;

        private Rect imageRect;

        private int updateBitmapFlag;

        public FrameRenderer(CanvasControl view)
        {
            this.view = view;
        }

        public event Action<Rect> ImageRectChanged;

        public Rect ImageRect
        {
            get => imageRect;

            private set
            {
                if (value != imageRect)
                {
                    imageRect = value;
                    ImageRectChanged?.Invoke(imageRect);
                }
            }
        }

        public bool IsReady => currentFrame != null;

        public ILutEffectGenerator LutEffect { get; set; }

        public void Dispose()
        {
            currentFrame?.Dispose();
        }

        public void Draw(CanvasDrawingSession session, double wW, double wH, double aspect)
        {
            if (currentFrame != null)
            {
                lock (frameDrawLock)
                {
                    var iW = currentFrame.Bitmap.SizeInPixels.Width;
                    var iH = currentFrame.Bitmap.SizeInPixels.Height / aspect;

                    var scaleX = wW / iW;
                    var scaleY = wH / iH;

                    var scale = Math.Min(scaleX, scaleY);

                    var rH = iH * scale;
                    var rW = iW * scale;

                    ImageRect = new Rect((wW - rW) / 2, (wH - rH) / 2, rW, rH);
                    currentFrame.Draw(session, imageRect);
                }
            }
            else
            {
                session.Clear(Colors.Transparent);
            }
        }

        public void Reset()
        {
            var cur = currentFrame;
            currentFrame = null;
            if (cur != null)
            {
                var task = Task.Run(() =>
                {
                    try
                    {
                        lock (frameDrawLock)
                        {
                            cur.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(new Exception("Frame dispose failed", ex));
                    }
                });
            }
        }

        public async Task<CameraPoint?> UpdateBitmap(Stream stream)
        {
            if (Interlocked.CompareExchange(ref updateBitmapFlag, 1, 0) == 0)
            {
                try
                {
                    stream.Position = 0;

                    var newframe = new FrameData();
                    ICanvasImage content = newframe.Bitmap = await CanvasBitmap.LoadAsync(view, stream.AsRandomAccessStream());
                    if (LutEffect != null)
                    {
                        content = newframe.LutImage = LutEffect.GenerateEffect(content);
                    }

                    newframe.Content = content;

                    var oldframe = currentFrame;
                    currentFrame = newframe;

                    if (oldframe != null)
                    {
                        var task = Task.Run(() =>
                        {
                            try
                            {
                                lock (frameDrawLock)
                                {
                                    oldframe.Dispose();
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error(new Exception("Frame dispose failed", ex));
                            }
                        });
                    }

                    view.Invalidate();
                    return new CameraPoint((int)newframe.Bitmap.SizeInPixels.Width, (int)newframe.Bitmap.SizeInPixels.Height);
                }
                finally
                {
                    updateBitmapFlag = 0;
                }
            }

            return null;
        }

        private class FrameData : IDisposable
        {
            public CanvasBitmap Bitmap { get; set; }

            public ICanvasImage Content { private get; set; }

            public ICanvasImage LutImage { private get; set; }

            public void Dispose()
            {
                Bitmap?.Dispose();
                LutImage?.Dispose();
            }

            public void Draw(CanvasDrawingSession session, Rect imageRect)
            {
                session.DrawImage(Content, imageRect, new Rect(new Point(0, 0), Bitmap.Size), 1.0f, CanvasImageInterpolation.NearestNeighbor);
            }
        }
    }
}