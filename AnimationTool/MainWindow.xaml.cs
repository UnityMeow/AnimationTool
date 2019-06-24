using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Timers;
using System.Drawing;
using System.Drawing.Imaging;

namespace AnimationTool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        CMove move;
        System.Timers.Timer timer;
        bool On;
        int index;
        int GameFrame;
        string bpath;
        string dpath;
        public MainWindow()
        {
            bpath = "";
            dpath = "";
            InitializeComponent();
            On = false;
            index = 0;
            GameFrame = 0;
            timer = new System.Timers.Timer();
            //设置定时器调用时间间隔
            timer.Interval = 1000 / 60;
            timer.Enabled = true;
            //委托
            timer.Elapsed += Updata;
        }
        //间隔固定时间 执行一次该函数 
        public void Updata(object sender, ElapsedEventArgs e)
        {
            //安全检测
            if (move == null)
                return;
            //是否在播放动画
            if (!On)
                return;
            GameFrame++;
            if (GameFrame >= move.GetFPS() && index < move.GetPicLength() - 1)
            {
                index++;
                GameFrame = 0;
            }
            this.Dispatcher.Invoke
                (
                    new Action
                    (
                        delegate
                        {
                            if ((bool)Box.IsChecked && index >= move.GetPicLength() - 1)
                            {
                                index = 0;
                            }
                            if (!(bool)Box.IsChecked && index >= move.GetPicLength() - 1)
                            {
                                On = false;
                                index = 0;
                            }
                            Pic.Source = move.GetPicData(index);
                        }
                    )
                 );
        }

        void Button_Click(object sender, RoutedEventArgs e)
        {
            //文件夹选择窗口
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = bpath;
            fbd.ShowDialog();
            //获取当前选择路径
            bpath = fbd.SelectedPath;
            if (bpath == "")
                return;
            move = new CMove(bpath);
            Pic.Source = move.GetPicData(0);

            //Pic.Source = new BitmapImage(new Uri(Finfo[2].FullName));
        }

        void WriteMoveName(object sender, TextChangedEventArgs e)
        {
            if (move == null)
                return;
            move.SetMoveName(MoveName.Text);
        }

        void WriteMoveFps(object sender, TextChangedEventArgs e)
        {
            if (move == null )
                return;
            if (MoveFps.Text == "")
                return;
             move.SetFPS(int.Parse(MoveFps.Text));
        }

        void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (move == null)
                return;
            On = !On;
            if (On && (bool)Box.IsChecked)
                play.Content = "停止";
            else
                play.Content = "播放";
        }

        void Button_Click_2(object sender, RoutedEventArgs e)
        {
            //===================================================================================
            //弹出窗口
            //System.Windows.MessageBox.Show()
            //===================================================================================
            if (move == null)
                return;
            if (MoveName.Text == "" || MoveFps.Text == "")
                System.Windows.MessageBox.Show("动作名称或帧率尚未填写");
            //裁剪图片有效区域
            //通过裁剪范围得到大矩形中的位置范围信息
            MaxRectsBinPack Canvas = new MaxRectsBinPack(2048, 2048);
            int len = move.GetPicLength();
            //将图片源信息 转换为bitmap
            Bitmap[] Picbmp = new Bitmap[len];
            //得到图片有效区
            Int32Rect[] imgRects = new Int32Rect[len];
            //得到大矩形中的位置范围信息
            Int32Rect[] drawRects = new Int32Rect[len];
            BitmapImage[] PicData = new BitmapImage[len];
            //得到图片的锚点
            int[] anchorx = new int[len];
            int[] anchory = new int[len];
            for (int i = 0; i < len; i++)
            {
                PicData[i] = move.GetPicData(i);
                Picbmp[i] = GetBitmpa(PicData[i]);
                imgRects[i] = GetTransparentBounds(PicData[i]);
                drawRects[i] = Canvas.insert(imgRects[i].Width, imgRects[i].Height, FreeRectangleChoiceHeuristic.BottomLeftRule);
                anchorx[i] = PicData[i].PixelWidth / 2 - imgRects[i].X;
                anchory[i] = PicData[i].PixelHeight / 2 - imgRects[i].Y;
            }
            int picw = 0;
            int pich = 0;
            //if(drawRects.Length == 1)
            //{
            //    picw = picw = drawRects[0].X + drawRects[0].Width + 5;
            //    pich = drawRects[0].Y + drawRects[0].Height + 5;
            //}
            for (int i = 0; i < drawRects.Length; i++)
            {
                picw = Math.Max(picw, drawRects[i].X + drawRects[i].Width + 5);
                pich = Math.Max(pich, drawRects[i].Y + drawRects[i].Height + 5);
                //if (drawRects[i].X <= drawRects[i + 1].X)
                //{
                //    picw = drawRects[i + 1].X + drawRects[i + 1].Width + 5;
                //}
                //if (drawRects[i].Y <= drawRects[i + 1].Y)
                //{
                //    pich = drawRects[i + 1].Y + drawRects[i + 1].Height + 5;
                //}
            }
            Bitmap BigPic = CombinImage(PicData, imgRects, drawRects, picw, pich);

            //文件夹选择窗口
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = dpath;
            fbd.ShowDialog();
            //获取当前选择路径
            dpath = fbd.SelectedPath;
            if (dpath == "")
                return;
            BigPic.Save(dpath + "\\"+ move.GetMoveName() + ".png", ImageFormat.Png);
            using (FileStream fs = new FileStream(dpath + "\\" + move.GetMoveName() + ".bytes", FileMode.Create))
            {

                BinaryWriter bw = new BinaryWriter(fs);
                Stream tar = bw.BaseStream;
                tar.Write(BitConverter.GetBytes(len), 0, BitConverter.GetBytes(len).Length);
                for (int i = 0; i < len; i++)
                {
                    tar.Write(BitConverter.GetBytes(drawRects[i].X), 0, BitConverter.GetBytes(drawRects[i].X).Length);
                    tar.Write(BitConverter.GetBytes(drawRects[i].Y), 0, BitConverter.GetBytes(drawRects[i].Y).Length);
                    tar.Write(BitConverter.GetBytes(drawRects[i].Width), 0, BitConverter.GetBytes(drawRects[i].Width).Length);
                    tar.Write(BitConverter.GetBytes(drawRects[i].Height), 0, BitConverter.GetBytes(drawRects[i].Height).Length);
                    tar.Write(BitConverter.GetBytes(anchorx[i]), 0, BitConverter.GetBytes(anchorx[i]).Length);
                    tar.Write(BitConverter.GetBytes(anchory[i]), 0, BitConverter.GetBytes(anchory[i]).Length);
                }
                tar.Write(BitConverter.GetBytes(move.GetFPS()), 0, BitConverter.GetBytes(move.GetFPS()).Length);
                bw.Flush();
                bw.Close();
            }
        }
 
        Bitmap GetBitmpa(BitmapSource img)
        {
            Bitmap bmp = new Bitmap(img.PixelWidth, img.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            BitmapData data = bmp.LockBits(
            new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            img.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }
        // 得到透明界限
        Int32Rect GetTransparentBounds(BitmapSource source)
        {
            Bitmap bmp = GetBitmpa(source);
            //图片宽
            int width = source.PixelWidth;
            //图片高
            int height = source.PixelHeight;
            //像素字节
            var pixelBytes = new byte[height * width * 4];
            //得到图片像素信息
            source.CopyPixels(pixelBytes, width * 4, 0);
            //四条线
            int? leftX = null, rightX = null, upY = null, downY = null;
            Int32Rect result = new Int32Rect(0, 0, 0, 0);
            //横向遍历像素的alpha值
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte alpha = pixelBytes[(y * width + x) * 4 + 3];
                    if (alpha > 0)
                    {
                        //获取左右边界
                        if (leftX.HasValue)
                        {
                            leftX = Math.Min(leftX.Value, x);
                            rightX = Math.Max(rightX.Value, x);
                        }
                        else
                        {
                            leftX = x;
                            rightX = x;
                        }
                        //获取上下边界
                        if (upY.HasValue)
                        {
                            upY = Math.Min(upY.Value, y);
                            downY = Math.Max(downY.Value, y);
                        }
                        else
                        {
                            upY = y;
                            downY = y;
                        }
                    }
                };
            }
            //得到透明界限
            if (leftX.HasValue && upY.HasValue)
            {
                if (leftX.Value >= 5)
                    leftX -= 5;
                if (upY.Value >= 5)
                    upY -= 5;

                result.X = leftX.Value;
                result.Y = upY.Value;

                rightX += 5;
                if (rightX > width)
                    rightX = width;
                downY += 5;
                if (downY > height)
                    downY = height;

                result.Width = rightX.Value - leftX.Value;
                result.Height = downY.Value - upY.Value;
            }

            return result;
        }
        //将小图绘制到大图上
        Bitmap CombinImage(BitmapImage[] data, Int32Rect[] imgRects, Int32Rect[] drawRects, int w, int h)
        {
            Bitmap bmp = new Bitmap(w, h);
            Graphics g = Graphics.FromImage(bmp);
            g.Clear(System.Drawing.Color.Transparent);
            CroppedBitmap img;
            //Int32Rect imgRect;
            //Int32Rect drawRect;
            for (int i = 0, j = 0; i < data.Length; ++i)
            {
                //imgRect = imgRects[i];
                if (imgRects[i].Width == 0)
                    continue;
                //drawRect = drawRects[j];
                img = new CroppedBitmap(data[i], new Int32Rect(imgRects[i].X, imgRects[i].Y, imgRects[i].Width, imgRects[i].Height));
                g.DrawImage(GetBitmpa(img), drawRects[j].X, drawRects[j].Y, drawRects[j].Width, drawRects[j].Height);
                ++j;
            }
            GC.Collect();
            return bmp;
        }
    }
}
