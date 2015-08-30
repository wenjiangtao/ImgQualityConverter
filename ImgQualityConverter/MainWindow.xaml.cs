using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImgQualityConverter
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 私有成员声明

        private ImageCodecInfo[] _CodecInfos = ImageCodecInfo.GetImageEncoders();
        private ImageCodecInfo _JpgCodecInfo = null;
        private ImageCodecInfo _PngCodecInfo = null;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            InitializeCodecInfos();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        #region 事件处理

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MainWindowViewModel.Instance.Save();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = MainWindowViewModel.Instance;
        }

        private void BtnPath_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindowViewModel.Instance.IsConvertForMultiFile)
            {
                var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
                folderDialog.ShowDialog();
                if (folderDialog.SelectedPath != "")
                {
                    MainWindowViewModel.Instance.Path = folderDialog.SelectedPath;
                }
            }
            else
            {
                var fileDialog = new System.Windows.Forms.OpenFileDialog();
                fileDialog.InitialDirectory = MainWindowViewModel.Instance.Path;
                fileDialog.Filter = "Supported Image Files(*.JPG;*.PNG)|*.JPG;*.PNG";
                fileDialog.ShowDialog();
                if (fileDialog.FileName != "")
                {
                    MainWindowViewModel.Instance.Path = fileDialog.FileName;
                }
            }
        }

        private void TxtQuality_TextChanged(object sender, TextChangedEventArgs e)
        {
            int value = 0;
            var txt = (sender as TextBox).Text;
            txt = txt == "" ? "1" : txt;
            try
            {
                value = Int32.Parse(txt);
            }
            catch
            {
                (sender as TextBox).Text = MainWindowViewModel.Instance.Quality.ToString();
                return;
            }
            
            value = Math.Max(1, Math.Min(value, 100));

            MainWindowViewModel.Instance.Quality = value;
        }

        private void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindowViewModel.Instance.IsConvertForSingleFile)
            {
                FileInfo fileInfo = new FileInfo(MainWindowViewModel.Instance.Path);
                if (IsFileLegal(fileInfo))
                {
                    ConvertForSingleFile(fileInfo);
                    if (!CheckBak.IsChecked.Value)
                    {
                        ClearBak(fileInfo.Directory);
                    }
                    MessageBox.Show("转换完成！");
                }
                else
                {
                    MessageBox.Show("请输入合法图片文件路径！");
                }
            }
            else
            {
                DirectoryInfo dirInfo = new DirectoryInfo(MainWindowViewModel.Instance.Path);
                if (dirInfo.Exists)
                {
                    var sum = ConvertForMultiFile(dirInfo);
                    if (!CheckBak.IsChecked.Value)
                    {
                        ClearBak(dirInfo);
                    }
                    MessageBox.Show("转换完成" + sum + "个文件！");
                }
                else
                {
                    MessageBox.Show("目录不存在！");
                }
            }
        }

        private void BtnRevertConfig_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel.Instance.Revert();
        }

        private void BtnRecovery_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo dirInfo =
                MainWindowViewModel.Instance.IsConvertForMultiFile ? new DirectoryInfo(MainWindowViewModel.Instance.Path) : new FileInfo(MainWindowViewModel.Instance.Path).Directory;

            if (!IsBakExists(dirInfo))
            {
                MessageBox.Show("不存在任何备份目录！");
                return;
            }

            var res = MessageBox.Show("将通过备份目录进行文件还原，将覆盖可能的原有文件！");

            var sum = Recovery(dirInfo);
            ClearBak(dirInfo);
            MessageBox.Show("通过备份还原" + sum + "个文件！");
        }

        private void BtnClearBak_Click(object sender, RoutedEventArgs e)
        {
            DirectoryInfo dirInfo =
                MainWindowViewModel.Instance.IsConvertForMultiFile ? new DirectoryInfo(MainWindowViewModel.Instance.Path) : new FileInfo(MainWindowViewModel.Instance.Path).Directory;

            if (!IsBakExists(dirInfo))
            {
                MessageBox.Show("不存在任何备份目录！");
                return;
            }

            ClearBak(dirInfo);
        }

        #endregion

        #region 私有方法

        private void InitializeCodecInfos()
        {


            foreach (var eachCodecInfo in _CodecInfos)
            {
                if (eachCodecInfo.FormatDescription.Equals("JPEG"))
                {
                    _JpgCodecInfo = eachCodecInfo;
                }
                if (eachCodecInfo.FormatDescription.Equals("PNG"))
                {
                    _PngCodecInfo = eachCodecInfo;
                }
            }
        }

        /// <summary>
        /// 文件路径是否有效
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        private bool IsFileLegal(FileInfo fileInfo)
        {
            return fileInfo.Exists && !IsFileFromBak(fileInfo) && (fileInfo.Extension.ToLower() == ".jpg" || fileInfo.Extension.ToLower() == ".jpeg" || fileInfo.Extension.ToLower() == ".png");
        }

        /// <summary>
        /// 获取编码器参数
        /// </summary>
        /// <returns></returns>
        private EncoderParameters GetEncoderParameters()
        {
            EncoderParameters encoderParams = new EncoderParameters();
            EncoderParameter encoderParam = new EncoderParameter(Encoder.Quality, MainWindowViewModel.Instance.Quality);
            encoderParams.Param[0] = encoderParam;
            return encoderParams;
        }

        /// <summary>
        /// 转换单个文件
        /// </summary>
        /// <param name="fileInfo"></param>
        private void ConvertForSingleFile(FileInfo fileInfo)
        {
            var dirBak = fileInfo.Directory.CreateSubdirectory("bak");
            var bakFileInfo = fileInfo.CopyTo(dirBak.FullName + @"\" + fileInfo.Name, true);
            fileInfo.Delete();

            var image = System.Drawing.Image.FromFile(bakFileInfo.FullName);
            var dirTmp = fileInfo.Directory.CreateSubdirectory("tmp");
            ConvertImgToJPG(image, dirTmp.FullName + @"\1.jpg");
            ConvertImgToPNG(image, dirTmp.FullName + @"\2.png");
            ConvertImgToPNGWithHighQuality(image, dirTmp.FullName + @"\3.png");
            image.Dispose();
            
            FileInfo minFile = bakFileInfo;
            long minFileSize = minFile.Length;
            foreach (var eachFile in dirTmp.GetFiles())
            {
                if (eachFile.Length < minFileSize)
                {
                    minFile = eachFile;
                    minFileSize = eachFile.Length;
                }
            }

            minFile.CopyTo(fileInfo.Directory + @"\" + fileInfo.Name.Replace(fileInfo.Extension, minFile.Extension), true);
            minFile.CopyTo(dirBak.CreateSubdirectory("new").FullName + @"\" + fileInfo.Name.Replace(fileInfo.Extension, minFile.Extension), true);
            dirTmp.Delete(true);
        }

        /// <summary>
        /// 将image转换为高质量PNG
        /// </summary>
        /// <param name="image">需要转换的image</param>
        /// <param name="path">输出路径</param>
        private void ConvertImgToPNGWithHighQuality(System.Drawing.Image image, string path)
        {
            System.Drawing.Image bmpImage = new System.Drawing.Bitmap(image.Width, image.Height);
            Graphics g = Graphics.FromImage(bmpImage);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            if (!System.Drawing.Image.IsAlphaPixelFormat(image.PixelFormat))
            {
                g.Clear(System.Drawing.Color.White);
            }
            g.DrawImage(image, new System.Drawing.Rectangle(0, 0, image.Width, image.Height), new System.Drawing.Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
            bmpImage.Save(path, ImageFormat.Png);
            bmpImage.Dispose();
        }

        /// <summary>
        /// 将image转换为PNG
        /// </summary>
        /// <param name="image">需要转换的image</param>
        /// <param name="path">输出路径</param>
        private void ConvertImgToPNG(System.Drawing.Image image, string path)
        {
            image.Save(path, _PngCodecInfo, GetEncoderParameters());
        }

        /// <summary>
        /// 将image转换为JPG（如果image含透明像素，将不做任何操作）
        /// </summary>
        /// <param name="image">需要转换的image</param>
        /// <param name="path">输出路径</param>
        private void ConvertImgToJPG(System.Drawing.Image image, string path)
        {
            if (image.RawFormat.Guid == ImageFormat.Png.Guid)
            {
                if (!System.Drawing.Image.IsAlphaPixelFormat(image.PixelFormat))
                {
                    System.Drawing.Image bmpImage = new System.Drawing.Bitmap(image.Width, image.Height);
                    Graphics g = Graphics.FromImage(bmpImage);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.Clear(System.Drawing.Color.White);
                    g.DrawImage(image, new System.Drawing.Rectangle(0, 0, image.Width, image.Height), new System.Drawing.Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);
                    bmpImage.Save(path, _JpgCodecInfo, GetEncoderParameters());
                    bmpImage.Dispose();
                }
            }
            else if (image.RawFormat.Guid == ImageFormat.Jpeg.Guid)
            {
                image.Save(path, _JpgCodecInfo, GetEncoderParameters());
            }
        }

        //private bool IsImgAlpha(System.Drawing.Image image)
        //{
        //    Bitmap bmp = new Bitmap(image);
        //    for (var x = 0; x < bmp.Width; x++)
        //    {
        //        for (var y = 0; y < bmp.Height; y++)
        //        {
        //            if (bmp.GetPixel(x, y).A != 255)
        //            {
        //                return true;
        //            }
        //        }
        //    }
        //    return false;
        //}

        /// <summary>
        /// 转换整个目录（含子目录）
        /// </summary>
        /// <param name="dirInfo"></param>
        /// <returns></returns>
        private int ConvertForMultiFile(DirectoryInfo dirInfo)
        {
            int sum = 0;
            foreach (var eachFileInfo in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                if (IsFileLegal(eachFileInfo))
                {
                    ConvertForSingleFile(eachFileInfo);
                    sum++;
                }
            }
            return sum;
        }

        /// <summary>
        /// 备份目录是否存在（检查包含子目录）
        /// </summary>
        /// <param name="dirInfo"></param>
        /// <returns></returns>
        private bool IsBakExists(DirectoryInfo dirInfo)
        {
            if (dirInfo.Exists)
            {
                return dirInfo.GetDirectories("bak", SearchOption.AllDirectories).Count() > 0;
            }
            return false;
        }

        /// <summary>
        /// 清除备份目录（包含子目录中的备份目录）
        /// </summary>
        /// <param name="dirInfo"></param>
        private void ClearBak(DirectoryInfo dirInfo)
        {
            foreach (var eachBakDir in dirInfo.GetDirectories("bak", SearchOption.AllDirectories))
            {
                if (System.IO.Directory.Exists(eachBakDir.FullName))
                {
                    try
                    {
                        eachBakDir.Delete(true);
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// 通过备份目录还原（包含子目录中的备份目录）
        /// </summary>
        /// <param name="dirInfo"></param>
        /// <returns></returns>
        private int Recovery(DirectoryInfo dirInfo)
        {
            var sum = 0;
            foreach (var eachBakDir in dirInfo.GetDirectories("bak", SearchOption.AllDirectories))
            {
                var baksnews = eachBakDir.GetDirectories("new", SearchOption.AllDirectories);
                if (baksnews.Count() > 0)
                {
                    foreach (var eachBakNewFile in baksnews[0].GetFiles())
                    {
                        var newFile = new FileInfo(eachBakNewFile.FullName.Replace(@"bak\new\", ""));
                        if (newFile.Exists)
                        {
                            newFile.Delete();
                        }
                    }
                }

                foreach (var eachFile in eachBakDir.GetFiles())
                {
                    eachFile.CopyTo(eachFile.FullName.Replace(@"bak\", ""), true);
                    sum++;
                }
                
            }
            return sum;
        }

        /// <summary>
        /// 文件是否来自备份目录
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        private bool IsFileFromBak(FileInfo fileInfo)
        {
            return (fileInfo.Directory.FullName.IndexOf("bak") != -1);
        }

        #endregion
    }
}
