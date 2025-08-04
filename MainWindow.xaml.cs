using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using FellowOakDicom;

namespace DicomDe_identificationTools
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseInput_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Browser input click");
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                InputPathBox.Text = dialog.FileName;
            }
        }

        private void BrowseOutput_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Browser output click");

            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                OutputPathBox.Text = dialog.FileName;
            }
        }

        private async void StartAnonymize_Click(object sender, RoutedEventArgs e)
        {
            string inputPath = InputPathBox.Text;
            string outputPath = OutputPathBox.Text;

            if (!Directory.Exists(inputPath) || !Directory.Exists(outputPath))
            {
                Log("输入或输出路径无效");
                return;
            }

            string[] files = Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories);
            int total = files.Length, current = 0;
            Progress.Maximum = total;
            Log($"共发现 {total} 个 DICOM 文件，开始处理...");

            await Task.Run(() =>
            {
                foreach (var file in files)
                {
                    try
                    {
                        var dicom = DicomFile.Open(file);
                        string accession = dicom.Dataset.GetSingleValueOrDefault(DicomTag.AccessionNumber, "UNKNOWN");
                        string seriesNumber = dicom.Dataset.GetSingleValueOrDefault(DicomTag.SeriesNumber, "0");
                        string seriesDesc = dicom.Dataset.GetSingleValueOrDefault(DicomTag.SeriesDescription, "NO_DESC");

                        string subFolder = System.IO.Path.Combine(outputPath, accession, $"{seriesNumber}_{seriesDesc.Replace('\\', '_')}");
                        Directory.CreateDirectory(subFolder);

                        AnonymizeDicom(dicom);
                        string outPath = System.IO.Path.Combine(subFolder, System.IO.Path.GetFileName(file));
                        dicom.Save(outPath);

                        Dispatcher.Invoke(() =>
                        {
                            current++;
                            Progress.Value = current;
                            Log($"处理完成: {file}");
                        });
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => Log($"错误: {file} - {ex.Message}"));
                    }
                }
            });

            Log("全部处理完成");

        }

        private void AnonymizeDicom(DicomFile dicom)
        {
            var ds = dicom.Dataset;

            //patient info
            ds.AddOrUpdate(DicomTag.PatientName, "Anonymized");
            ds.Remove(DicomTag.PatientAddress);
            ds.Remove(DicomTag.PatientBirthDate);
            ds.Remove(DicomTag.StudyID);


            //institution info
            ds.AddOrUpdate(DicomTag.InstitutionName, "Anonymized");
            ds.Remove(DicomTag.InstructionDescription);
            ds.Remove(DicomTag.InstitutionAddress);

            //physician info
            ds.AddOrUpdate(DicomTag.PerformingPhysicianName, "Anonymized");
            ds.Remove(DicomTag.ReferringPhysicianName);
            ds.Remove(DicomTag.ReferringPhysicianAddress);
            ds.Remove(DicomTag.ReferringPhysicianTelephoneNumbers);
            ds.Remove(DicomTag.ReferringPhysicianIdentificationSequence);

        }


        private void Log(string message)
        {
            LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            LogBox.ScrollToEnd();
        }
    }
}
