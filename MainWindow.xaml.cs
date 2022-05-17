using ImageMagick;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ImageFormatConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Queue<string> Files { get; set; }
        private int TotalFilesCount { get; set; } = 0;
        private int CurrentFile { get; set; } = 1;

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void BtnProcessFiles_Click(object sender, RoutedEventArgs e)
        {
            this.ProcessFiles();
        }

        private void BtnSelectFiles_Click(object sender, RoutedEventArgs e)
        {
            this.SelectFiles();
        }

        private void SelectFiles()
        {
            var ofd = new OpenFileDialog();
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == true)
            {
                var files = ofd.FileNames;

                if (files.Length > 0)
                {
                    this.Files = new Queue<string>();

                    foreach (var file in files)
                    {
                        if (File.Exists(file))
                        {
                            this.Files.Enqueue(file);
                        }
                    }

                    this.txtTotalFilesCount.Text = this.Files.Count.ToString();
                }
            }
        }

        private void ProcessFiles()
        {
            if (this.IsContinue())
            {
                this.txtStatus.Text = "Processando arquivos...";
                this.TotalFilesCount = this.Files.Count;
                this.CurrentFile = 1;

                this.ProcessNextFileAsync(this.Files.Dequeue());
            }
            else
            {
                MessageBox.Show("Você precisa selecionar os arquivos.");
            }
        }

        private Task ProcessNextFileAsync(string file)
        {
            return Task.Factory.StartNew(() =>
            {
                this.ProcessNextFile(file);
            });
        }

        private void ProcessNextFile(string file)
        {
            this.AlterImageFormat(file);

            this.UpdateProgress(this.CurrentFile, this.TotalFilesCount);
            this.CurrentFile++;


            if(this.Files.Count > 0)
            {
                this.ProcessNextFile(this.Files.Dequeue());
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Arquios processados com sucesso!");
                    
                    this.txtTotalFilesCount.Text = "0";
                    this.pgProgress.Value = 0;
                    this.txtStatus.Text = "Aguardando...";
                });
            }
        }

        private void AlterImageFormat(string file)
        {
            try
            {
                using (var image = new MagickImage(file))
                {
                    var format = this.GetImageFormat();
                    image.Format = format;

                    this.SaveImage(image, file);
                }
            }
            catch
            {
                this.CopyOriginalFile(file);
            }
        }

        private void UpdateProgress(double currentFile, double totalFilesCount)
        {
            this.Dispatcher.InvokeAsync(() => 
            {
                var progress = (currentFile / totalFilesCount) * 100D;
                this.pgProgress.Value = progress;
            });
        }

        private void SaveImage(MagickImage image, string file)
        {
            var destinationFolder = this.GetDestinationFolder(file);
            var fileName = $"{Path.GetFileNameWithoutExtension(file)}.{image.Format.ToString().ToLower()}";

            var filePath = Path.Combine(destinationFolder, fileName);

            image.Quality = 92;
            image.Write(filePath);
        }

        private void CopyOriginalFile(string file)
        {
            var destinationFolder = this.GetDestinationFolder(file);
            var fileName = Path.GetFileName(file);

            var filePath = Path.Combine(destinationFolder, fileName);

            File.Copy(file, filePath);
        }

        private string GetDestinationFolder(string file)
        {
            var partialPath = Path.GetDirectoryName(file);
            var destinationPath = Path.Combine(partialPath, "_processados");

            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            return destinationPath;
        }

        private MagickFormat GetImageFormat()
        {
            if (!this.CheckAccess())
            {
                return this.Dispatcher.Invoke(() =>
                {
                    return this.GetImageFormat();
                });
            }

            var selectedFormat = ((ComboBoxItem)this.cmbFormats.SelectedItem).Tag.ToString();
            return this.GetImageFormat(selectedFormat);
        }

        private MagickFormat GetImageFormat(string selectedFormat)
        {
            switch (selectedFormat)
            {
                case "0":
                    return MagickFormat.Jpeg;

                case "1":
                    return MagickFormat.Png;

                case "2":
                    return MagickFormat.Tiff;

                default:
                    throw new NotSupportedException();
            }
        }

        private bool IsContinue()
        {
            return this.Files != null && this.Files.Count > 0;
        }
    }
}
