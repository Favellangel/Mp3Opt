using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.ComponentModel;
using System.Threading;
using System.Reflection;


namespace Mp3_opt
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            labelAssembly.Content = "version: " + Convert.ToString(Assembly.GetExecutingAssembly().GetName().Version);
        }

        private void dfs(string rootDir, in Stack<string> treeDirs)
        {
            Stack<string> dirs = new Stack<string>();
            if (!System.IO.Directory.Exists(rootDir))   // если папки не существует генерируем исключение
                throw new ArgumentException();
            dirs.Push(rootDir);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                treeDirs.Push(currentDir);
                string[] subDirs;
                try
                {
                    subDirs = System.IO.Directory.GetDirectories(currentDir);
                }

                catch (UnauthorizedAccessException e)
                {
                    MessageBox.Show("Каталог не доступен");
                    continue;
                }
                catch (System.IO.DirectoryNotFoundException e)
                {
                    MessageBox.Show("Каталог не найден");
                    continue;
                }
                    foreach (string str in subDirs)
                        dirs.Push(str);
            }
        }
        
        private void copyFile(FileInfo fileinf)
        {
            string locTmp;
            DirectoryInfo dirOut = new DirectoryInfo(TextBoxOutPath.Text);
            // получение пути (от вершины до имени файла)
            locTmp = fileinf.Directory.FullName;
            locTmp = locTmp.Remove(0, locTmp.IndexOf(TextBoxTop.Text));
            // копирование каталога до файла
            dirOut.CreateSubdirectory(locTmp);
            // копирование файла
            //fileinf.CopyTo(dirOut.FullName + '\\' + tmp + '\\' + fileinf.Name, true);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.AppStarting;
            string tmp = "";
            Stack<string> treeDirs = new Stack<string>();
            FileInfo tmpFI = null;           
            // получение жанров муз файлов
            tmp = TextBoxGenres.Text;
            string[] genres = tmp.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            dfs(TextBoxInPath.Text, in treeDirs);
            // выставляем значения прогресБара           
            PgsBar.Maximum = treeDirs.Count;
            // обход всех файлов во всех каталогах
            while (treeDirs.Count > 0)
            {
                string[] files = null;
                FileInfo fi = null;
                bool findMp3 = false;
                try
                {
                    files = System.IO.Directory.GetFiles(treeDirs.Pop());
                }

                catch (UnauthorizedAccessException ex)
                {

                    MessageBox.Show("файл не доступен");
                    return;
                }

                catch (System.IO.DirectoryNotFoundException ex)
                {
                    MessageBox.Show("не найден каталог в котором лежит файл");
                    return;
                }

                foreach (string file in files)
                {
                    try
                    {
                        fi = new System.IO.FileInfo(file);                        
                    }
                    catch (System.IO.FileNotFoundException ex)
                    {
                        MessageBox.Show("Файл не найден");
                        return;
                    }
                    catch(System.IO.IOException ex)
                    {
                        MessageBox.Show("Файл не найден или не доступен");
                    }

                    if (System.IO.Path.GetExtension(fi.FullName) == ".mp3")
                    {
                        var fileMp3 = TagLib.File.Create(fi.FullName);
                        /* действие первое. Переименовывание файлов
                        -------------------------------------------*/
                        if (CheckBoxRename.IsChecked.Value)
                        {
                            if (fileMp3.Tag.Title != null && fileMp3.Tag.FirstAlbumArtist != null)
                            {
                                string path;
                                string[] keywords = { "noscreamo", "female", "metalcore", "piano", "djent", "djentrus", "femalerus" };
                                // получаю путь без имени файла
                                path = fi.FullName;
                                path = path.Remove(fi.FullName.IndexOf(fi.Name));
                                //полный путь к файлу и его новое название 
                                path = path +
                                       fileMp3.Tag.FirstAlbumArtist +
                                       " - " + fileMp3.Tag.Title; 
                                // ищу совпадение по ключевым словам
                                foreach (string keyword in keywords)
                                {
                                    if (fi.Name.IndexOf(keyword) != -1)
                                    {
                                        path += "(" + keyword + ")";
                                        break;
                                    }
                                }
                                path += ".mp3";
                                //переименновывание файла
                                fi.MoveTo(path);
                            }
                        }
                        /* действие второе. Изменение картинок
                          -------------------------------------------*/
                        if (CheckBoxResize.IsChecked.Value)
                        {
                            TextBoxGenres.AppendText(fileMp3.Tag.Pictures + "\n");                           
                        }
                        /* действие третье. Копирование файлов по жанрам
                          -------------------------------------------*/
                        if (TextBoxGenres.Text.Length != 0)
                        {
                            /* FileStream fstream = File.OpenWrite(@"E:\New\log.txt");
                             byte[] array = System.Text.Encoding.Default.GetBytes(fi.FullName);
                             fstream.Write(array, 0, array.Length);
                             fstream.Close();*/
                            for (int i = 0; i < fileMp3.Tag.Genres.Length; i++)
                                for (int j = 0; j < genres.Length; j++)
                                    if (genres[j] == fileMp3.Tag.Genres[i])
                                    {
                                        copyFile(fi);
                                        findMp3 = true;
                                    }
                            //TextBoxGenres.AppendText(fi.FullName + "\n");
                        }
                        /* действие четвертое. поиск файлов без мета информации
                          -------------------------------------------*/
                    }
                    else if (fi.Name == "folder.jpg")
                    {
                        tmpFI = fi;
                    }
                }
                if (findMp3) // если есть песня удовлетворяющая условию в текущем каталоге, то копируем folder
                {
                    copyFile(tmpFI);
                    findMp3 = false;
                }                    
                PgsBar.Value++;
            }
            Cursor = Cursors.Arrow;
        }
    }
}
