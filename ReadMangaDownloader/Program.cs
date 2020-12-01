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
    public struct Command
    {
        public string Name;
        public string Description;
        public Regex RegexPattern;
        private Action<MangaInfo, Match> _actionCommand;

        public void Excecute(MangaInfo manga, string value)
        {
            _actionCommand?.Invoke(manga, RegexPattern.Match(value));
        }
        public bool IsThisCommand(string value) => RegexPattern.IsMatch(value);

        public Command(string name, string description, Regex regex, Action<MangaInfo, Match> excecute)
        {
            Name = name;
            Description = description;
            RegexPattern = regex;
            _actionCommand = excecute;
        }
    }

    class Program
    {
        //https://readmanga.live/dvorianstvo
        //https://readmanga.live/martial_peak

        private const ConsoleColor _baseColor = ConsoleColor.White;

        private static string _rootUrl;

        private static List<string> _webSites = new List<string>()
        {
            "readmanga.live",
            "mintmanga.live", 
            "allhentai.ru"
        };

        private static List<Command> _titleCommands = new List<Command>()
        {
            { new Command("info", "Информация о произведении",
                new Regex(@"\s*info\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                (manga, match)=>
                {
                    ShowMessageLine($"{manga.Names}: {manga.Name}\nОписание:\n" +
                                $"{manga.Description}");
                })
            },

            { new Command("chapters", "Показать список глав",
                new Regex(@"\s*chapters\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                (manga, match)=>
                {
                    int index = 0;
                    foreach (MangaChapter chapter in manga.Chapters)
                    {
                        ShowMessageLine($"({++index}) ", ConsoleColor.Yellow);
                        ShowMessage(chapter.Name);
                    }
                })
            },

            { new Command("count chapters", "Количество глав",
                new Regex(@"\s*count\s+chapters\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                (manga, match)=>
                {
                    ShowMessageLine($"Количество глав: {manga.Chapters.Count}");
                })
            },

            { new Command("last chapter info", "Информация по последней главе",
                new Regex(@"\s*last\s+chapter\s+info\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                (manga, match)=>
                {
                    var chapter = manga.Chapters[manga.Chapters.Count - 1];
                    ShowMessageLine($"Название: {chapter.Name}");
                })
            },
            { new Command("chapter [number] info", "Информация по главе [number]",
                new Regex(@"^chapter\s+(\d+)\s+info\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                (manga, match)=>
                {
                    int count = int.Parse(match.Groups[1].Value);
                    try
                    {
                        ShowMessageLine($"Название: {manga.Chapters[count - 1].Name}");
                    }
                    catch (Exception e)
                    {
                        ShowMessageLine(e.Message, ConsoleColor.Red);
                    }
                })
            },
            { new Command("download [*from-to] [*directory]: ", "Загрузить главы от [from] до [to] в директорию",
                new Regex(@"^download(\s+(\d+)(\s*-\s*(\d+))?)?\s*([A-Za-z]:\\.*)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled),
                (manga, match)=>
                {
                    int from = int.Parse(string.IsNullOrWhiteSpace(match.Groups[2].Value) == true ?
                                    "1" : match.Groups[2].Value);
                    int to;

                    if (string.IsNullOrWhiteSpace(match.Groups[4].Value))
                    {
                        to = string.IsNullOrWhiteSpace(match.Groups[2].Value) == true ? manga.Chapters.Count : from;
                    }
                    else
                        to = int.Parse(match.Groups[4].Value);

                    string directory = match.Groups[5].Value ?? string.Empty;

                    try
                    {
                        bool isDownloadOnlyChapter = from == to;
                        string rangeMessage = $"{ (from == to ? "главы " + from.ToString() : "глав " + from + " - " + to) }";
                        string directoryMessage = $"{(directory == string.Empty ? Environment.CurrentDirectory : directory)}!";
                        string message = $"Загрузка {rangeMessage} в директорию {directoryMessage}";

                        ShowMessageLine(message, ConsoleColor.Green);
                        manga.SaveFromTo(from - 1, to - 1, directory, 5);
                        ShowMessageLine($"{(isDownloadOnlyChapter == true ? "Глава загружена" : "Главы загружены")}", ConsoleColor.Green);
                    }
                    catch (Exception e)
                    {
                        ShowMessageLine(e.Message, ConsoleColor.Red);
                    }
                })
            }
        };
        
        static void Main(string[] args)
        {
            Console.Title = "ReadMangaDownloader";
            MangaInfo manga;
            while (true)
            {
                int index = 1;
                ShowMessage($"Доступные источники:\n");
                foreach (string item in _webSites)
                {
                    ShowMessage($"{index++}", ConsoleColor.Yellow);
                    ShowMessage($" - {item}\n");
                }
                ShowMessageLine("Выберите источник: ", ConsoleColor.Cyan);
                string webSiteIndex = Console.ReadLine();

                if (int.TryParse(webSiteIndex, out int select_index) 
                    && select_index <= _webSites.Count && select_index >= 1)
                {
                    _rootUrl = "https://" + _webSites[select_index - 1] + "/";
                    Console.Title = _webSites[select_index - 1]; ;
                }
                else
                {
                    ShowMessage("Неккоректный ввод!\n", ConsoleColor.Yellow);
                    continue;
                }

                while (true)
                {
                    string command;
                    string url = string.Empty;

                    ShowMessageLine($"Введите URL адрес: \n", ConsoleColor.Cyan);
                    ShowMessageLine(_rootUrl);
                    command = Console.ReadLine();
                    
                    if (Regex.IsMatch(command, @"^return\s+/c\s*$"))
                    {
                        break;
                    }
                    else if (Regex.IsMatch(command,@"^[^/]+$"))
                    {
                        if (Regex.IsMatch(command, @"https:\/\/[a-zA-Z0-9]+\.[A-Za-z]+\/.+ "))
                            url = command;
                        else
                            url = _rootUrl + command;
                    }
                    else
                    {
                        ShowMessage("Неккоректный ввод\n", ConsoleColor.Red);
                        continue;
                    }
                    
                    try
                    {
                        ShowMessage("Загрузка...", ConsoleColor.Green);
                        manga = new MangaInfo(url);
                        manga.GetChapters();

                        while (true)
                        {
                            ShowMessageLine($"\n{url}/", ConsoleColor.Cyan);
                            command = Console.ReadLine();

                            if (command == "commands")
                            {
                                ShowCommandsList();
                            }
                            else if (command == "return")
                            {
                                break;
                            }
                            else
                            {
                                bool isTitleCommand = false;
                                foreach(Command cmd in _titleCommands)
                                {
                                    if (cmd.IsThisCommand(command))
                                    {
                                        cmd.Excecute(manga, command);
                                        isTitleCommand = true;
                                        break;
                                    }   
                                }
                                if (!isTitleCommand)
                                {
                                    ShowMessage("Неизвестная комманда!\n", ConsoleColor.Yellow);
                                    ShowCommandsList();
                                }
                            }
                        }
                    }

                    catch (Exception e)
                    {
                        ShowMessageLine(e.Message + "\n", ConsoleColor.Red);
                    }
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
            ShowMessage("Ввернутся к вводу URL");

            foreach (Command item in _titleCommands)
            {
                ShowMessageLine($"{item.Name}", ConsoleColor.Yellow);
                ShowMessage($"{item.Description}");
            }
        }
    }
}
