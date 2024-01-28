using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Downloader;
using CmlLib.Core.Files;
using CmlLib.Core.Version;
using CmlLib.Core.VersionLoader;
using CmlLib.Core.VersionMetadata;


namespace FoxLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {
        private static MainWindow? Instance;
        public ObservableCollection<string> Versions { get; set; } = new ObservableCollection<string>();
        Progress<DownloadFileChangedEventArgs> downloadProgress = new Progress<DownloadFileChangedEventArgs>();
        Progress<ProgressChangedEventArgs> fileProgress = new Progress<ProgressChangedEventArgs>();

        IDownloader downloaderSec = new SequenceDownloader();
        string clients = Path.Combine(Environment.CurrentDirectory, "clients.txt");


        public MainWindow()
        {
            InitializeComponent();
            ServicePointManager.DefaultConnectionLimit = 256;
            Instance = this;
            DataContext = this;
            HideTabItem("Выбор клиента");
            HideTabItem("Загрузка клиента");
        }

        private void HideTabItem(string tabHeader)
        {
            TabItem tabItem = tabControl.Items.Cast<TabItem>().FirstOrDefault(item => item.Header.ToString() == tabHeader);
            if (tabItem != null)
            {
                tabItem.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowTabItem(string tabHeader)
        {
            TabItem tabItem = tabControl.Items.Cast<TabItem>().FirstOrDefault(item => item.Header.ToString() == tabHeader);
            if (tabItem != null)
            {
                tabItem.Visibility = Visibility.Visible;
            }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (versionListBox.SelectedItem != null)
            {
                UpdateRichTextBoxAsync($"Выбрана версия: {versionListBox.SelectedItem.ToString()}");
                textBlockVer.Text = versionListBox.SelectedItem.ToString();

                TabItem desiredTab = tabControl.Items.Cast<TabItem>().FirstOrDefault(item => item.Header.ToString() == "Основное");
                Properties.Settings.Default.LastSelectedClient = versionListBox.SelectedItem.ToString();
                Properties.Settings.Default.Save();
                if (desiredTab != null)
                {
                    // Выберите вкладку
                    tabControl.SelectedItem = desiredTab;
                }
                ShowTabItem("Логи");
                ShowTabItem("Основное");
                ShowTabItem("Настройки");
                HideTabItem("Выбор клиента");
            }
            else
            {
                // Ничего не выбрано, обработайте ситуацию по умолчанию
                System.Windows.MessageBox.Show("Клиент не выбран");
            }
        }

        public static async void UpdateRichTextBoxAsync(string message)
        {
            if (Instance != null)
            {
                await Instance.richTextBoxDebug.Dispatcher.InvokeAsync(() =>
                {
                    Instance.richTextBoxDebug.AppendText($"{DateTime.Now} - {message}\n");
                    Instance.richTextBoxDebug.ScrollToEnd();
                });
            }
        }

        private void LoadSettings()
        {
            // Загрузка клиента
            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastSelectedClient))
            {
                UpdateRichTextBoxAsync($"Установка выбранного клиента - {Properties.Settings.Default.LastSelectedClient}");
                textBlockVer.Text = Properties.Settings.Default.LastSelectedClient;
            }
            // Загрузка никнейма
            if (string.IsNullOrEmpty(Properties.Settings.Default.LastEnteredNickname))
            {
                UpdateRichTextBoxAsync($"Никнейм не обнаружен в памяти, установка значения по умолчанию - 'User' и сохранение в памяти для последующих запусков");
                textBoxNickname.Text = "User";
                Properties.Settings.Default.LastEnteredNickname = "User";
            }
            else
            {
                UpdateRichTextBoxAsync($"Никнейм обнаружен в памяти, установка значения {Properties.Settings.Default.LastEnteredNickname}");
                textBoxNickname.Text = Properties.Settings.Default.LastEnteredNickname;
            }

            // Загрузка пути к папке
            if (string.IsNullOrEmpty(Properties.Settings.Default.LastSelectedFolderPath))
            {
                UpdateRichTextBoxAsync($"Путь не обнаружен в памяти, установка значения...");

                var folderDialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "Выберите папку"
                };

                System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    UpdateRichTextBoxAsync($"Установлен путь '{folderDialog.SelectedPath}' и сохранено значение в памяти");
                    Properties.Settings.Default.LastSelectedFolderPath = folderDialog.SelectedPath;
                    textBlockDir.Text = folderDialog.SelectedPath;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    UpdateRichTextBoxAsync("Отмена");
                }
            }
            else
            {
                UpdateRichTextBoxAsync($"Путь '{Properties.Settings.Default.LastSelectedFolderPath}' обнаружен в памяти, установка значения");
                textBlockDir.Text = Properties.Settings.Default.LastSelectedFolderPath;
            }

            if (Properties.Settings.Default.LastEnteredRAM != sliderRAM.Value)
            {
                // Устанавливаем значение слайдера только если оно отличается
                sliderRAM.Value = Properties.Settings.Default.LastEnteredRAM;

                textBlockRAM.Text = $"{sliderRAM.Value} MB ({sliderRAM.Value / 1024} ГБ)";
            }
        }

        private async void Window_Loaded(object sender, EventArgs e)
        {
            try
            {
                LoadSettings();
                DownloadFile[] files = { new DownloadFile(clients, "http://92.255.108.96/clients.txt") };
                await downloaderSec.DownloadFiles(files, downloadProgress, fileProgress);

                if (System.IO.File.Exists(clients))
                {
                    using (var stream = System.IO.File.OpenRead(clients))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string line;
                            while ((line = await reader.ReadLineAsync()) != null)
                            {
                                // Проверка наличия элемента в коллекции
                                if (!Versions.Contains(line))
                                {
                                    // Обновление UI в основном потоке
                                    this.Dispatcher.Invoke(() => Versions.Add(line));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateRichTextBoxAsync($"Произошла ошибка: {ex.Message}");
            }
        }

        private void SliderRAM_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                if (Properties.Settings.Default.LastEnteredRAM != Convert.ToInt32(sliderRAM.Value))
                {
                    if (textBlockRAM != null)
                    {
                        // Обновляем текст в TextBlock с текущим значением слайдера
                        textBlockRAM.Text = $"{sliderRAM.Value} MB ({sliderRAM.Value / 1024} ГБ)";
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateRichTextBoxAsync($"Ошибка при сохранении значения слайдера: {ex.Message}");
            }
        }

        private void buttonNickname_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(textBoxNickname.Text))
                {
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.LastEnteredNickname))
                    {
                        UpdateRichTextBoxAsync($"Никнейм не введён, установка последнего введённого значения - {Properties.Settings.Default.LastEnteredNickname} и сохранение в памяти для последующих запусков");
                        textBoxNickname.Text = Properties.Settings.Default.LastEnteredNickname;
                    }
                    else
                    {
                        UpdateRichTextBoxAsync($"Никнейм не введён, установка значения по умолчанию - 'User' и сохранение в памяти для последующих запусков");
                        textBoxNickname.Text = "User";
                        Properties.Settings.Default.LastEnteredNickname = "User";
                    }

                }
                else
                {
                    UpdateRichTextBoxAsync($"Никнейм {textBoxNickname.Text} сохранён в памяти и будет использоваться при последующих запусках");
                    Properties.Settings.Default.LastEnteredNickname = textBoxNickname.Text;
                }
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                UpdateRichTextBoxAsync($"Ошибка: {ex.Message}");
            }
        }

        private async void buttonStart_Click(object sender, RoutedEventArgs e)
        {

            // Логика для кнопки "Запуск без обновления"
            if (textBlockDir.Text != "" && textBlockVer.Text != "")
            {
                string data = Path.Combine(textBlockDir.Text, $"{textBlockVer.Text}/data");
                string games = Path.Combine(textBlockDir.Text, $"{textBlockVer.Text}/games");
                if (Directory.Exists(data) && Directory.Exists(games))
                {
                    bool work = false;
                    await StartLauncherAsync(work);
                    Close();
                }
                else
                {
                    textBlockFile.Text = "";
                    HideTabItem("Основное");
                    HideTabItem("Настройки");
                    HideTabItem("Выбор версии");
                    ShowTabItem("Загрузка клиента");
                    TabItem desiredTab = tabControl.Items.Cast<TabItem>().FirstOrDefault(item => item.Header.ToString() == "Загрузка клиента");

                    if (desiredTab != null)
                    {
                        // Выберите вкладку
                        tabControl.SelectedItem = desiredTab;
                    }
                    await DownloadAndUnzipFiles();
                    Close();
                }
            }
        }

        private async Task UnzipFiles(List<string> fileNames)
        {
            int totalFiles = fileNames.Count;
            int processedFiles = 0;

            foreach (var fileName in fileNames)
            {
                string localFilePath = Path.Combine(textBlockDir.Text, $"{textBlockVer.Text}/data/{fileName}");
                string destinationFolder = GetCategoryFolder(fileName); // Получаем подпапку в зависимости от типа архива
                string destinationPath = Path.Combine(textBlockDir.Text, $"{textBlockVer.Text}/games/{destinationFolder}");

                try
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(localFilePath, destinationPath);

                    // Обновление прогресса и текста в UI
                    processedFiles++;
                    double progress = (double)processedFiles / totalFiles * 100;

                    // Используйте Dispatcher.Invoke для обновления элементов UI в основном потоке
                    Dispatcher.Invoke(() =>
                    {
                        progressBar.Value = progress;
                        textBlockFile.Text = $"Файл: {fileName} ({processedFiles}/{totalFiles})";
                    });
                }
                catch (Exception ex)
                {
                    UpdateRichTextBoxAsync($"Ошибка при распаковке файла {fileName}: {ex.Message}");
                }
            }
        }

        // Метод, который возвращает подпапку в зависимости от типа архива
        private string GetCategoryFolder(string fileName)
        {
            string[] categories = { "assets", "mods", "libraries", "kubejs", "config", "scripts", "defaultconfigs", "versions" };

            foreach (var category in categories)
            {
                if (fileName.ToLower().Contains(category))
                {
                    return category;
                }
            }

            return "other"; // Если тип архива неизвестен, сохраняем в папке "other"
        }

        private async void buttonStart1_Click(object sender, RoutedEventArgs e)
        {
            // Логика для кнопки "Запуск с обновлением"
            textBlockFile.Text = "";
            HideTabItem("Основное");
            HideTabItem("Настройки");
            HideTabItem("Выбор версии");
            ShowTabItem("Загрузка клиента");
            TabItem desiredTab = tabControl.Items.Cast<TabItem>().FirstOrDefault(item => item.Header.ToString() == "Загрузка клиента");

            if (desiredTab != null)
            {
                // Выберите вкладку
                tabControl.SelectedItem = desiredTab;
            }
            await DownloadAndUnzipFiles();
            Close();
        }

        static long GetLocalFileSize(string filePath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                return fileInfo.Length;
            }
            catch (Exception ex)
            {
                UpdateRichTextBoxAsync($"Ошибка при получении размера локального файла: {ex.Message}");
                return -1;
            }
        }

        static long GetWebFileSize(string url)
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
                request.Method = "HEAD";
                using (WebResponse response = request.GetResponse())
                {
                    return response.ContentLength;
                }
            }
            catch (Exception ex)
            {
                UpdateRichTextBoxAsync($"Ошибка при получении размера файла с веб-сайта: {ex.Message}");
                return -1;
            }
        }

        private async Task DownloadAndUnzipFiles()
        {
            if (textBlockDir.Text != "" && textBlockVer.Text != "")
            {
                string dirArchive = Path.Combine(textBlockDir.Text, $"{textBlockVer.Text}/data");
                string url = "http://92.255.108.96/ftb/";

                List<DownloadFile> filesToDownload = new List<DownloadFile>();

                List<string> fileNames = new List<string>
            {
                "versions.zip",
                "assets.zip",
                "mods.zip",
                "libraries.zip",
                "kubejs.zip",
                "config.zip",
                "scripts.zip",
                "defaultconfigs.zip"
            };

                // Создаем экземпляр AsyncParallelDownloader перед циклом
                var downloaderPar = new AsyncParallelDownloader();

                foreach (var fileName in fileNames)
                {
                    string fileUrl = $"{url}{textBlockVer.Text}/{fileName}";
                    string localFilePath = Path.Combine(dirArchive, fileName);

                    if (!System.IO.File.Exists(localFilePath))
                    {
                        filesToDownload.Add(new DownloadFile(localFilePath, fileUrl));
                    }
                    else
                    {
                        // Если файл существует, проверяем размеры
                        long webFileSize = GetWebFileSize(fileUrl);
                        long localFileSize = GetLocalFileSize(localFilePath);

                        if (webFileSize != localFileSize)
                        {
                            filesToDownload.Add(new DownloadFile(localFilePath, fileUrl));
                        }
                    }
                }

                DownloadFile[] files = filesToDownload.ToArray();

                fileProgress.ProgressChanged += OnProgressChanged;

                // Асинхронно запускаем скачивание файлов одним загрузчиком
                await downloaderPar.DownloadFiles(files, downloadProgress, fileProgress);
                await UnzipFiles(fileNames);
                if (!System.IO.File.Exists(Path.Combine(textBlockDir.Text, textBlockVer.Text, "games", "options.txt")))
                {
                    string OptlocalFilePath = Path.Combine(Path.Combine(textBlockDir.Text, textBlockVer.Text, "games", "options.txt"));
                    string fileUrlOpt = $"{url}{textBlockVer.Text}/options.txt";
                    DownloadFile[] opt = { new DownloadFile(OptlocalFilePath, fileUrlOpt) };
                    await downloaderSec.DownloadFiles(opt, downloadProgress, fileProgress);
                }
                string ServlocalFilePath = Path.Combine(Path.Combine(textBlockDir.Text, textBlockVer.Text, "games", "servers.dat"));
                string ServUrlOpt = $"{url}{textBlockVer.Text}/servers.txt";
                DownloadFile[] file = { new DownloadFile(ServlocalFilePath, ServUrlOpt) };
                await downloaderSec.DownloadFiles(file, downloadProgress, fileProgress);
                bool work = true;
                StartLauncherAsync(work);
            }
        }
        private ObservableCollection<MVersionMetadata> versionCollection;

        private async Task StartLauncherAsync(bool work)
        {
            try
            {
                string dir = Path.Combine(textBlockDir.Text, textBlockVer.Text, "games");
                var path = new MinecraftPath(dir);
                var launcher = new CMLauncher(path);
                var localVersionLoader = new LocalVersionLoader(path);

                MVersionCollection versionMetadataCollection = localVersionLoader.GetVersionMetadatas();
                versionCollection = new ObservableCollection<MVersionMetadata>(versionMetadataCollection);

                MVersionMetadata selectedVersion = versionCollection.FirstOrDefault(v => v.Name.IndexOf("forge", StringComparison.OrdinalIgnoreCase) != -1);

                launcher.FileChanged += (e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        // Обновляем TextBlock
                        textBlockFile.Text = $"Файл: {e.FileName}";

                        // Обновляем ProgressBar
                        progressBar.Value = (double)e.ProgressedFileCount / e.TotalFileCount * 100;
                    });
                };

                launcher.ProgressChanged += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        // Обновляем ProgressBar
                        progressBar.Value = e.ProgressPercentage;
                    });
                };

                if (selectedVersion != null)
                {
                    var process = await launcher.CreateProcessAsync(selectedVersion.Name, new MLaunchOption
                    {
                        MaximumRamMb = Properties.Settings.Default.LastEnteredRAM,
                        Session = MSession.GetOfflineSession($"{textBoxNickname.Text}"),
                    }, checkAndDownload: work);
                    process.Start();
                }
                else
                {
                    UpdateRichTextBoxAsync("Не удалось найти версию с 'forge' в названии.");
                }
            }
            catch (Exception ex)
            {
                UpdateRichTextBoxAsync($"Произошла ошибка: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        void OnProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                // Обновляем прогресс в UI (если требуется)
                progressBar.Value = (double)e.ProgressPercentage;
            });
        }

        private void buttonChooseDirectory_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Выберите папку"
            };

            System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                UpdateRichTextBoxAsync($"Установлен путь '{folderDialog.SelectedPath}' и сохранено значение в памяти");
                Properties.Settings.Default.LastSelectedFolderPath = folderDialog.SelectedPath;
                textBlockDir.Text = folderDialog.SelectedPath;
                textBlockVer.Text = "";
                Properties.Settings.Default.Save();
            }
            else
            {
                UpdateRichTextBoxAsync("Отмена");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void btnRAM_Click(object sender, RoutedEventArgs e)
        {
            UpdateRichTextBoxAsync($"Установка значения оперативной памяти {sliderRAM.Value} и сохранение в память");
            Properties.Settings.Default.LastEnteredRAM = Convert.ToInt32(sliderRAM.Value);
            textBlockRAM.Text = $"{sliderRAM.Value} MB ({sliderRAM.Value / 1024} ГБ)";
            Properties.Settings.Default.Save();
        }

        private void btnVer_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.LastSelectedFolderPath != null && versionListBox.Items.Count != 0)
            {
                try
                {
                    TabItem desiredTab = tabControl.Items.Cast<TabItem>().FirstOrDefault(item => item.Header.ToString() == "Выбор клиента");

                    if (desiredTab != null)
                    {
                        tabControl.SelectedItem = desiredTab;
                    }
                    HideTabItem("Логи");
                    HideTabItem("Основное");
                    HideTabItem("Настройки");
                    HideTabItem("Загрузка клиента");

                    // Показать нужные элементы
                    ShowTabItem("Выбор клиента");

                }
                catch (Exception ex)
                {
                    UpdateRichTextBoxAsync($"Произошла ошибка: {ex.Message}");
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Не выбран путь или нету доступных клиентов");
                UpdateRichTextBoxAsync("Не выбран путь или нету доступных версий");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists($"{textBlockFile.Text}/data"))
            {
                Directory.Delete($"{textBlockDir.Text}/{textBlockVer}/data");
                Directory.Delete($"{textBlockDir.Text}/{textBlockVer}/games");
            }
        }
    }
}
