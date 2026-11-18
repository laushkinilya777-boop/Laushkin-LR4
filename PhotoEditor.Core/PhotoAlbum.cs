using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PhotoEditor.Core
{
    /// <summary>Альбом — все открытые фотографии и номер выбранной. Это и есть "состояние" программы.</summary>
    public class PhotoAlbum
    {
        /// <summary>Список фотографий.</summary>
        public List<Picture> Photos { get; set; } = new List<Picture>();

        /// <summary>Номер выбранной фотографии (-1 — ничего не выбрано).</summary>
        public int SelectedIndex { get; set; } = -1;

        /// <summary>Выбранная фотография.</summary>
        public Picture GetSelected()
        {
            if (SelectedIndex < 0 || SelectedIndex >= Photos.Count)
                throw new InvalidOperationException("Фотография не выбрана.");
            return Photos[SelectedIndex];
        }

        /// <summary>Добавляет фотографию и сразу выбирает её.</summary>
        public void Add(Picture picture)
        {
            Photos.Add(picture);
            SelectedIndex = Photos.Count - 1;
        }

        /// <summary>Выбирает фотографию по номеру.</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= Photos.Count)
                throw new ArgumentException("Нет фотографии с таким номером.");
            SelectedIndex = index;
        }
    }

    /// <summary>Сохранение и загрузка альбома.</summary>
    public interface IAlbumStorage
    {
        void Save(PhotoAlbum album, string path);
        PhotoAlbum Load(string path);
    }

    /// <summary>Хранит альбом в файле JSON (сериализация System.Text.Json).</summary>
    public class JsonAlbumStorage : IAlbumStorage
    {
        public void Save(PhotoAlbum album, string path)
        {
            string json = JsonSerializer.Serialize(album);
            File.WriteAllText(path, json);
        }

        public PhotoAlbum Load(string path)
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<PhotoAlbum>(json);
        }
    }
}
