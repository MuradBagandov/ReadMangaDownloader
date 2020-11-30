using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ReadMangaDownloader
{
    class Program
    {
        //https://readmanga.live/dvorianstvo
        //https://readmanga.live/martial_peak

        private const ConsoleColor _baseColor = ConsoleColor.White;
        private const string _rootUrl = "https://readmanga.live/";
        static void Main(string[] args)
        {
            Console.Title = "ReadMangaDownloader";
            MangaInfo manga;
            while (true)
            {
                ShowMessage("Введите url адрес: ", ConsoleColor.Cyan);
                ShowMessage(_rootUrl);
                var url = _rootUrl+ Console.ReadLine();
                
                try
                {
                    ShowMessage("Загрузка...", ConsoleColor.Green);
                    manga = new MangaInfo(url);
                    manga.GetChapters();
                        
                    while (true)
                    {
                        ShowMessageLine($"\n{url}/", ConsoleColor.Cyan);
                        string command = Console.ReadLine();
                        Match mcommand;

                        if (command == "commands")
                        {
                            ShowCommandsList();
                        }
                        else if (command == "info")
                        {
                                ShowMessageLine($"{manga.Names}: {manga.Name}\nОписание:\n" +
                                $"{manga.Description}");
                        }
                        else if (command == "return")
                        {
                            break;
                        } 
                        else if (command == "chapters")
                        {
                            int index = 0;
                            foreach (MangaChapter chapter in manga.Chapters)
                            {
                                ShowMessageLine($"({++index}) ", ConsoleColor.Yellow);
                                ShowMessage(chapter.Name);
                            }
                        }
                        else if ((mcommand = Regex.Match(command, @"^download(\s+(\d+)(\s*-\s*(\d+))?)?\s*([A-Za-z]:\\.*)?$")).Success)
                        {
                            int from = int.Parse(string.IsNullOrWhiteSpace(mcommand.Groups[2].Value) == true?
                                "1": mcommand.Groups[2].Value);
                            int to;
                                
                            if (string.IsNullOrWhiteSpace(mcommand.Groups[4].Value))
                            {
                                to = string.IsNullOrWhiteSpace(mcommand.Groups[2].Value) == true ? manga.Chapters.Count : from;
                            }
                            else
                                to = int.Parse(mcommand.Groups[4].Value);

                            string directory = mcommand.Groups[5].Value??string.Empty;
                                
                            try 
                            {
                                bool isDownloadOnlyChapter = from == to;
                                string rangeMessage = $"{ (from == to ? "главы " + from.ToString() : "глав " + from + " - " + to) }";
                                string directoryMessage = $"{(directory == string.Empty ? Environment.CurrentDirectory : directory)}!";
                                string message = $"Загрузка {rangeMessage} в директорию {directoryMessage}";

                                ShowMessageLine(message, ConsoleColor.Green);
                                manga.SaveFromTo(from-1, to-1, directory, 5);
                                ShowMessageLine($"{(isDownloadOnlyChapter == true ? "Глава":"Главы")} загружены", ConsoleColor.Green);
                            }
                            catch (Exception e)
                            {
                                ShowMessageLine(e.Message, ConsoleColor.Red);
                            }
                        }
                        else if (command == "count chapters")
                        {
                            ShowMessageLine($"Количество глав: {manga.Chapters.Count}");
                        }
                        else if (command == "last chapter info")
                        {
                            var chapter = manga.Chapters[manga.Chapters.Count - 1];
                            ShowMessageLine($"Название: {chapter.Name}");
                        }
                        else if ((mcommand = Regex.Match(command, @"^chapter\s+(\d+)\s+info\s*$")).Success)
                        {
                            int count = int.Parse(mcommand.Groups[1].Value);
                            try
                            {
                                ShowMessageLine($"Название: {manga.Chapters[count-1].Name}");
                            }
                            catch (Exception e)
                            {
                                ShowMessageLine(e.Message, ConsoleColor.Red);
                            }
                        }
                        else
                        {
                            ShowMessage("Неизвестная комманда!\n", ConsoleColor.Yellow);
                            ShowCommandsList();
                        }  
                    }
                }

                catch (Exception e)
                {
                    ShowMessageLine(e.Message + "\n", ConsoleColor.Red);
                }
            }
        }

        static void ShowMessage(string value, ConsoleColor color = _baseColor)
        {
            Console.ForegroundColor = color;
            Console.Write(value);
            Console.ForegroundColor = _baseColor;
        }
        static void ShowMessageLine(string value, ConsoleColor color = _baseColor)
        {
            ShowMessage("\n"+value, color);
        }

        static void ShowCommandsList()
        {
            ShowMessageLine($"Комманды:");
            ShowMessageLine("return: ", ConsoleColor.Yellow);
            ShowMessage("Вернутся к вводу url адресса");
            ShowMessageLine("info: ", ConsoleColor.Yellow);
            ShowMessage("Информация о произведении");
            ShowMessageLine("chapters: ", ConsoleColor.Yellow);
            ShowMessage("Показать список глав");
            ShowMessageLine("count: ", ConsoleColor.Yellow);
            ShowMessage("Количество глав");
            ShowMessageLine("last chapter info: ", ConsoleColor.Yellow);
            ShowMessage("Информация по последней главе");
            ShowMessageLine("chapter [number] info: ", ConsoleColor.Yellow);
            ShowMessage("Информация по главе");
            ShowMessageLine("download [*directory]: ", ConsoleColor.Yellow);
            ShowMessage("Загрузить главы в директорию");
            ShowMessageLine("download [number] [*directory]: ", ConsoleColor.Yellow);
            ShowMessage("Загрузить главу [number] в директорию");
            ShowMessageLine("download [from] - [to] [*directory]: ", ConsoleColor.Yellow);
            ShowMessage("Загрузить главы от [from] до [to] в директорию");
        }
    }
}
