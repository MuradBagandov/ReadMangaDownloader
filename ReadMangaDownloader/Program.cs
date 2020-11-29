﻿using System;
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
        static void Main(string[] args)
        {
            MangaInfo manga;
            while (true)
            {
                Console.WriteLine("Введите url адресс: https://readmanga.live/*");
                var url = Console.ReadLine();
                
                try
                {
                    Console.WriteLine("Загрузка...");
                    manga = new MangaInfo(url);
                    manga.GetChapters();
                        
                    while (true)
                    {
                            Console.Write($"\n{url}\\");
                            string command = Console.ReadLine();
                            Match mcommand;

                            if (command == "commands")
                            {
                                Console.Write($"Комманды:" +
                                    $"\nreturn: Вернутся к вводу url адресса" +
                                    $"\ninfo: Информация о произведении" +
                                    $"\nchapters: Показать список глав" +
                                    $"\ncount chapters: Количество глав" +
                                    $"\nlast chapter info: Информация по последней главе" +
                                    $"\nchapter [number] info: Информация по главе" +
                                    $"\ndownload [directory]: Загрузить главы в директорию" +
                                    $"\ndownload [number] [directory]: Загрузить главу [number] в директорию" +
                                    $"\ndownload [from] - [to] [directory]: Загрузить главы от [from] до [to] в директорию" +
                                    $"\n");
                            }
                            if (command == "info")
                            {
                                Console.Write($"{manga.Names}: {manga.Name}\nОписание:\n" +
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
                                    Console.WriteLine($"({++index}) {chapter.Name}");
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
                                    Console.WriteLine($"Загрузка глав {from} - {to} в директорию " +
                                        $"{(directory == string.Empty ? Environment.CurrentDirectory: directory)}!");
                                    manga.SaveFromTo(from-1, to-1, directory, 5);
                                    Console.WriteLine($"Главы загружены");
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                }
                            }
                            else if (command == "count chapters")
                            {
                                Console.WriteLine($"Количество глав: {manga.Chapters.Count}");
                            }
                            else if (command == "last chapter info")
                            {
                                var chapter = manga.Chapters[manga.Chapters.Count - 1];
                                Console.WriteLine($"Название: {chapter.Name}");
                            }
                            else if ((mcommand = Regex.Match(command, @"^chapter\s+(\d+)\s+info\s*$")).Success)
                            {
                                int count = int.Parse(mcommand.Groups[1].Value);
                                try
                                {
                                    Console.WriteLine($"Название: {manga.Chapters[count-1].Name}");
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                }
                            }

                            else
                                Console.WriteLine("Неизвестная комманда!");
                        }
                    }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
}
