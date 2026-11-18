using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PhotoEditor.Core;
using DrawingColor = System.Drawing.Color;

namespace PhotoEditor.Wpf
{
    /// <summary>
    /// Главное окно фоторедактора (ЛР4). Использует те же классы, что и консольная версия:
    /// Picture, фильтры IFilter, PhotoAlbum и JsonAlbumStorage.
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string StateFile = "album.json";
        private readonly IAlbumStorage storage = new JsonAlbumStorage();
        private PhotoAlbum album = new PhotoAlbum();

        public MainWindow()
        {
            InitializeComponent();
        }

        // ---------- состояние: загрузка при запуске, сохранение при закрытии ----------

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (File.Exists(StateFile))
            {
                album = storage.Load(StateFile);
                StatusText.Text = "Состояние восстановлено.";
            }
            Refresh();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            storage.Save(album, StateFile);
        }

        // ---------- вспомогательные методы ----------

        /// <summary>Обновляет список фото и картинку на экране.</summary>
        private void Refresh()
        {
            int selected = album.SelectedIndex;
            PhotoList.ItemsSource = null;
            PhotoList.ItemsSource = album.Photos;
            PhotoList.SelectedIndex = selected;

            if (selected >= 0)
            {
                Picture p = album.GetSelected();
                // int в формате ARGB лежит в памяти как байты B, G, R, A — это формат Bgra32
                PhotoView.Source = BitmapSource.Create(p.Width, p.Height, 96, 96, PixelFormats.Bgra32, null, p.Pixels, p.Width * 4);
            }
            else
            {
                PhotoView.Source = null;
            }
        }

        /// <summary>Выполняет действие над выбранным фото; ошибки показывает в окне.</summary>
        private void Edit(Action<Picture> action)
        {
            try
            {
                action(album.GetSelected());
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private static int Number(TextBox box) => int.Parse(box.Text);

        private DrawingColor SelectedColor => DrawingColor.FromName(((ComboBoxItem)ColorBox.SelectedItem).Content.ToString());

        // ---------- обработчики кнопок ----------

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp" };
            if (dialog.ShowDialog() == true)
            {
                album.Add(Picture.Load(dialog.FileName));
                Refresh();
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog { Filter = "PNG|*.png", FileName = "photo.png" };
            if (dialog.ShowDialog() == true)
                Edit(p => p.Save(dialog.FileName));
        }

        private void PhotoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PhotoList.SelectedIndex >= 0 && PhotoList.SelectedIndex != album.SelectedIndex)
            {
                album.Select(PhotoList.SelectedIndex);
                Refresh();
            }
        }

        private void Crop_Click(object sender, RoutedEventArgs e) =>
            Edit(p => p.Crop(Number(CropX), Number(CropY), Number(CropW), Number(CropH)));

        private void Rotate_Click(object sender, RoutedEventArgs e) => Edit(p => p.RotateRight());

        private void Lighter_Click(object sender, RoutedEventArgs e) => Edit(p => p.ApplyFilter(new BrightnessFilter(40)));
        private void Darker_Click(object sender, RoutedEventArgs e) => Edit(p => p.ApplyFilter(new BrightnessFilter(-40)));
        private void Gray_Click(object sender, RoutedEventArgs e) => Edit(p => p.ApplyFilter(new GrayscaleFilter()));
        private void Invert_Click(object sender, RoutedEventArgs e) => Edit(p => p.ApplyFilter(new InvertFilter()));
        private void Sepia_Click(object sender, RoutedEventArgs e) => Edit(p => p.ApplyFilter(new SepiaFilter()));

        private void Text_Click(object sender, RoutedEventArgs e) =>
            Edit(p => p.AddText(LabelText.Text, Number(TextX), Number(TextY), SelectedColor, Number(TextSize)));

        private void Collage_Click(object sender, RoutedEventArgs e)
        {
            Edit(p =>
            {
                Picture second = album.Photos[Number(CollageWith) - 1];
                album.Add(Picture.MakeCollage(p, second));
            });
        }

        /// <summary>Щелчок по фото в режиме рисования меняет цвет пикселя.</summary>
        private void PhotoView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DrawMode.IsChecked != true || album.SelectedIndex < 0) return;
            Picture p = album.GetSelected();
            Point pos = e.GetPosition(PhotoView);
            // переводим координаты на экране в координаты пикселя картинки
            int x = (int)(pos.X * p.Width / PhotoView.ActualWidth);
            int y = (int)(pos.Y * p.Height / PhotoView.ActualHeight);
            Edit(pic => pic.SetPixel(x, y, SelectedColor));
            StatusText.Text = $"Пиксель ({x}, {y}) закрашен.";
        }
    }
}
