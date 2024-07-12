using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

public class SteamApiHelper
{
    private static readonly string apiKey;
    private static readonly string baseUrl;

    static SteamApiHelper()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        IConfigurationRoot configuration = builder.Build();

        apiKey = configuration["ApiKey"];
        baseUrl = configuration["BaseUrl"];
    }

    public static async Task<JObject> GetOwnedGamesAsync(string steamId, bool includeAppInfo = true,
        bool includePlayedFreeGames = false)
    {
        string url =
            $"{baseUrl}/IPlayerService/GetOwnedGames/v1/?key={apiKey}&steamid={steamId}&include_appinfo={includeAppInfo}&include_played_free_games={includePlayedFreeGames}&format=json";
        using (HttpClient client = new HttpClient())
        {
            Console.WriteLine(url);
            var response = await client.GetStringAsync(url);
            return JObject.Parse(response);
        }
    }

    public static async Task<JObject> GetUserStatsForGameAsync(string steamId, string appId)
    {
        string url = $"{baseUrl}/ISteamUserStats/GetUserStatsForGame/v2/?key={apiKey}&steamid={steamId}&appid={appId}";

        using (HttpClient client = new HttpClient())
        {
            try
            {
                var response = await client.GetStringAsync(url);
                return JObject.Parse(response);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
        }
    }

    public static async Task<JObject> GetPlayerAchievementsAsync(string steamId, string appId)
    {
        string url =
            $"{baseUrl}/ISteamUserStats/GetPlayerAchievements/v1/?key={apiKey}&steamid={steamId}&appid={appId}&l=english";

        using (HttpClient client = new HttpClient())
        {
            try
            {
                string response = await client.GetStringAsync(url);

                JObject jsonResponse = JObject.Parse(response);

                if (jsonResponse["playerstats"]?["error"] != null)
                {
                    string errorMessage = jsonResponse["playerstats"]["error"].ToString();
                    Console.WriteLine($"Error: {errorMessage}");
                    return null;
                }

                return jsonResponse;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
        }
    }

    public static async Task<JObject> GetSchemaForGameAsync(string appId)
    {
        string url = $"{baseUrl}/ISteamUserStats/GetSchemaForGame/v2/?key={apiKey}&appid={appId}&l=english";

        using (HttpClient client = new HttpClient())
        {
            try
            {
                string response = await client.GetStringAsync(url);
                return JObject.Parse(response);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
        }
    }

    public static async Task<JObject> GetPlayerAchievementsWithIconsAsync(string steamId, string appId)
    {
        var achievementsData = await GetPlayerAchievementsAsync(steamId, appId);
        var schemaData = await GetSchemaForGameAsync(appId);

        if (achievementsData != null && schemaData != null)
        {
            var achievements = achievementsData["playerstats"]["achievements"];
            var schemaAchievements = schemaData["game"]["availableGameStats"]["achievements"];

            foreach (var achievement in achievements)
            {
                var apiname = achievement["apiname"].ToString();
                var schemaAchievement = schemaAchievements.FirstOrDefault(a => a["name"].ToString() == apiname);

                if (schemaAchievement != null)
                {
                    achievement["icon"] = schemaAchievement["icon"];
                    achievement["icongray"] = schemaAchievement["icongray"];
                }
            }
        }

        return achievementsData;
    }

    public static async Task<JObject> GetPlayerSummariesAsync(string steamId)
    {
        string url = $"{baseUrl}/ISteamUser/GetPlayerSummaries/v2/?key={apiKey}&steamids={steamId}";
        using (HttpClient client = new HttpClient())
        {
            var response = await client.GetStringAsync(url);
            return JObject.Parse(response);
        }
    }

    public static async Task<JObject> GetSupportedAPIListAsync()
    {
        string url = $"{baseUrl}/ISteamWebAPIUtil/GetSupportedAPIList/v1/?key={apiKey}&format=json";
        using (HttpClient client = new HttpClient())
        {
            var response = await client.GetStringAsync(url);
            return JObject.Parse(response);
        }
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Добро пожаловать в моё приложение для визуализации статистики из steam!");

        bool isWork = true;

        while (isWork)
        {
            Console.WriteLine("Выберите интересующую вас опцию");

            Console.WriteLine("1 - Вывести список игр");
            Console.WriteLine("2 - Вывести статистику выбранной игры");
            Console.WriteLine("3 - Вывести достижения выбранной игры");
            Console.WriteLine("4 - Вывести сведения об игроке");
            Console.WriteLine("5 - Выйти из приложения");

            string userInput = Console.ReadLine();

            bool canParse = int.TryParse(userInput, out int menuItem);

            if (canParse)
            {
                switch (menuItem)
                {
                    case 1:
                        GetOwnedGamesAsync();
                        break;

                    case 2:
                        GetPlayerStatsForGameAsync();
                        break;

                    case 3:
                        GetPlayerAchievementsWithIconsAsync();
                        break;

                    case 4:
                        GetPlayerSummariesAsync();
                        break;

                    case 5:
                        isWork = false;
                        break;

                    default:
                        Console.WriteLine("Такой команды мы не знаем...");
                        break;
                }
            }
            else
            {
                Console.WriteLine("Вы ввели некорректный пункт");
            }
        }
    }

    public static async void GetOwnedGamesAsync()
    {
        Console.Write("Введите ваш steamID: ");
        string steamId = Console.ReadLine();
        var gamesData = await SteamApiHelper.GetOwnedGamesAsync(steamId);

        Console.WriteLine("Games owned by the user:");

        foreach (var game in gamesData["response"]["games"])
        {
            string appId = game["appid"].ToString();
            string name = game["name"].ToString();
            string playtimeForever = game["playtime_forever"].ToString();
            string imgIconUrl =
                $"http://media.steampowered.com/steamcommunity/public/images/apps/{appId}/{game["img_icon_url"]}.jpg";
            string imgLogoUrl =
                $"http://media.steampowered.com/steamcommunity/public/images/apps/{appId}/{game["img_logo_url"]}.jpg";

            Console.WriteLine($"Game: {name}");
            Console.WriteLine($"AppID: {appId}");
            Console.WriteLine($"Playtime (forever): {playtimeForever} minutes");
            Console.WriteLine($"Icon URL: {imgIconUrl}");
            Console.WriteLine($"Logo URL: {imgLogoUrl}");
            Console.WriteLine(new string('-', 20));
        }
    }

    public static async void GetPlayerStatsForGameAsync()
    {
        Console.Write("Введите ваш steamID: ");
        string steamId = Console.ReadLine();

        Console.Write("Введите appID игры, чтобы получить статистику: ");
        string selectedAppId = Console.ReadLine();

        var statsData = await SteamApiHelper.GetUserStatsForGameAsync(steamId, selectedAppId);

        Console.WriteLine("Статистика для выбранной игры:");

        foreach (var stat in statsData["playerstats"]["stats"])
        {
            string name = stat["name"].ToString();
            string value = stat["value"].ToString();

            Console.WriteLine($"Name: {name}, Value: {value}");
        }

        Console.WriteLine("Достижения выбранной игры:");

        foreach (var achievement in statsData["playerstats"]["achievements"])
        {
            string apiname = achievement["name"].ToString();
            bool achieved = achievement["achieved"].ToObject<bool>();
            string achievedStr = achieved ? "Yes" : "No";

            Console.WriteLine($"Достижение: {apiname}, получено: {achievedStr}");
        }
    }

    public static async void GetPlayerAchievementsWithIconsAsync()
    {
        Console.Write("Введите ваш steamID: ");
        string steamId = Console.ReadLine();
        Console.Write("Введите appID игры, чтобы получить достижения: ");
        string appId = Console.ReadLine();

        JObject achievementsData = await SteamApiHelper.GetPlayerAchievementsWithIconsAsync(steamId, appId);

        if (achievementsData != null && achievementsData["playerstats"] != null)
        {
            Console.WriteLine("Достижения выбранной игры:");

            if (achievementsData["playerstats"]["achievements"] != null)
            {
                foreach (JToken achievement in achievementsData["playerstats"]["achievements"])
                {
                    string name = achievement["name"].ToString();
                    bool achieved = achievement["achieved"].ToObject<bool>();
                    string achievedStr = achieved ? "Yes" : "No";
                    string iconUrl = achievement["icon"]?.ToString();
                    string iconGrayUrl = achievement["icongray"]?.ToString();

                    Console.WriteLine($"Достижение: {name}, получено: {achievedStr}");
                    Console.WriteLine($"Иконка: {iconUrl}");
                    Console.WriteLine($"Иконка без цвета: {iconGrayUrl}");
                }
            }
            else
            {
                Console.WriteLine("У игры нет достижений.");
            }
        }
        else
        {
            Console.WriteLine("Ошибка при попытке получить достижения игры.");
        }
    }

    public static async void GetPlayerSummariesAsync()
    {
        Console.Write("Введите ваш steamID: ");
        string steamId = Console.ReadLine();
        var playerData = await SteamApiHelper.GetPlayerSummariesAsync(steamId);

        if (playerData != null && playerData["response"] != null)
        {
            foreach (var player in playerData["response"]["players"])
            {
                string steamid = player["steamid"].ToString();
                string personaname = player["personaname"].ToString();
                string profileurl = player["profileurl"].ToString();

                Console.WriteLine($"SteamID: {steamid}");
                Console.WriteLine($"Persona Name: {personaname}");
                Console.WriteLine($"Profile URL: {profileurl}");
                Console.WriteLine(new string('-', 20));
            }
        }
        else
        {
            Console.WriteLine("Failed to retrieve player data.");
        }
    }

    public static async void GetSupportedAPIListAsync()
    {
        var apiList = await SteamApiHelper.GetSupportedAPIListAsync();

        if (apiList != null)
        {
            Console.WriteLine("Supported APIs:");

            foreach (var api in apiList["apilist"]["interfaces"])
            {
                string name = api["name"].ToString();
                Console.WriteLine($"API Name: {name}");
            }
        }
        else
        {
            Console.WriteLine("Failed to retrieve supported API list.");
        }
    }
}