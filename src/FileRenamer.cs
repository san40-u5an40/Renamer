using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

// Принцип работы программы:
// 
// Она принимает два аргумента
// - Флаг, который задаёт поведение
// - Путь к папке с файлами или к конкретному файлу для переименования
// 
// Флаги:
// -d - Путь указывает на директорию
// -f - Путь указывает на файл
// 
// При указании директории, все файлы в ней перебираются и переименовываются
// При указании конкретного файла переименовывается только он
// 
// Программа сначала проверяет корректность флагов,
// Если они корректны, передаёт управление соответствующему блоку программы
//
// Отдельную трудность представляет блок получения свободного имени файла:
// Т.к. в папке уже могут содержаться переименованные файлы,
// Необходимо сначала обратиться к их перечню, выявив свободное имя
// И только после этого переименовывать файл
//
// На данный момент не возникало ошибок, связанных с синхронизацией доступа к перечню этих файлов
// Поэтому на данный момент принято решение не использовать мьютексы
// Это замедлит производительность программы, особенно при большом количестве файлов в папке

internal static class FileRenamer
{
    private const string ARG_ERROR =
            "Необходимо сначала указать один из аргументов:\n" +
            "    -d — Для переименования всех файлов в указанной папке.\n" +
            "    -f — Для переименования одного переданного файла.\n\n" +
            "Затем ввести соответствующий путь с файлом или папкой.";

    // Провекра на флаги и управление ходом программы
    internal static void Rename(string[] args)
    {
        if (IsExist(args) || IsNullOrEmptyContains(args) || IsUncurrentFlag(args))
        {
            MessageBox(IntPtr.Zero, ARG_ERROR, "Renamer", MS_TYPE_CLASSIC);
            return;
        }

        string arg = args[0];
        string path = args[1];

        if (arg == "-d" && Directory.Exists(path))
        {
            DirRename(path);
        }
        else if (arg == "-f" && File.Exists(path))
        {
            FileRename(path);
        }
        else
        {
            MessageBox(IntPtr.Zero, "Указанного пути не существует!", "Renamer", MS_TYPE_CLASSIC);
            return;
        }
    }

    // Проверка на наличие аргументов
    private static bool IsExist(string[] args) => args.Length < 2;

    // Проверка на наличие пустых аргументов
    private static bool IsNullOrEmptyContains(string[] args) => args.Where(string.IsNullOrEmpty).Count() > 0;

    // Проверка на некорректные флаги
    private static bool IsUncurrentFlag(string[] args) => !(args[0] == "-d" || args[0] == "-f");

    // Метод для переименования всех файлов в одной папке
    private static void DirRename(string path)
    {
        var dir = new DirectoryInfo(path);
        var name = GetName(dir);

        try
        {
            foreach (var file in dir.GetFiles())
                FileMove(file, name, dir);

            MessageBox(IntPtr.Zero, "Файлы успешно переименованы!", "Renamer", MS_TYPE_CLASSIC);
        }
        catch (Exception ex)
        {
            MessageBox(IntPtr.Zero, $"Ошибка:\n" + ex.Message, "Renamer", MS_TYPE_CLASSIC | MS_ICON_ERROR);
        }
    }

    // Метод для переименования одного файла
    private static void FileRename(string path)
    {
        var file = new FileInfo(path);
        var dir = file.Directory;
        var name = GetName(dir!);

        try
        {
            FileMove(file, name, dir!);

            // Без уведомления, т.к. при массовом переименовании вылезет много окон
        }
        catch (Exception ex)
        {
            MessageBox(IntPtr.Zero, $"Ошибка:\n" + ex.Message, "Renamer", MS_TYPE_CLASSIC | MS_ICON_ERROR);
        }
    }

    // Метод получения имени нового файла по указанной директории
    private static string GetName(DirectoryInfo dir) =>
        dir.Name.ToLower().Replace(' ', '_');

    // Метод для переименования файла на основе нового имени и его расположения (чтобы проверить свободные названия)
    private static void FileMove(FileInfo file, string newFileName, DirectoryInfo dir)
    {
        string newPath = GetNewFilePath(file, newFileName, dir);
        file.MoveTo(newPath);
    }

    // Метод для получения нового свободного пути для файла:
    // 
    // Сначала формируется HashSet с именами всех созданных файлов в папке (без их расширения)
    // Далее цикл for перебирает потенциальные имена, начиная их нумерацию с 1
    // Если найдено свободное имя, цикл прерывается
    // И метод возвращает найденный свободный путь для переименованного файла
    private static string GetNewFilePath(FileInfo file, string newFileName, DirectoryInfo dir)
    {
        var files = dir
            .GetFiles()
            .Select(p => p.Name.Split('.')[0])
            .ToHashSet<string>();

        string newFileFullNameWithoutExtension = string.Empty;
        for (int i = 1; i < int.MaxValue; i++)
        {
            newFileFullNameWithoutExtension = i + "_" + newFileName;
            if(!files.TryGetValue(newFileFullNameWithoutExtension, out string? temp))
                break;
        }

        return Path.Combine(dir.FullName, newFileFullNameWithoutExtension + file.Extension);
    }

    // Импортируем MessageBox
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    // Тип окна
    private const uint MS_TYPE_CLASSIC = 0x00000000;

    // Иконка
    private const uint MS_ICON_ERROR = 0x00000010;
}