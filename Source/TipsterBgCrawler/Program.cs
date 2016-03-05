namespace TipsterBgCrawler
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using AngleSharp;
    using AngleSharp.Dom;

    public static class Program
    {
        private static IConfiguration configuration = Configuration.Default.WithDefaultLoader();
        private static IBrowsingContext browsingContext = BrowsingContext.New(configuration);

        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            while (true)
            {
                Console.WriteLine("Въведи адреса на двубоя от Tipster.bg:");
                var url = Console.ReadLine();
                var document = browsingContext.OpenAsync(url).Result;

                var homeTeamUrl = document.Origin + document.QuerySelectorAll(".gameStatLinkWrapper > a")[0].Attributes["href"].Value;
                var awayTeamUrl = document.Origin + document.QuerySelectorAll(".gameStatLinkWrapper > a")[1].Attributes["href"].Value;

                var homeTeam = GetAverageGoals(homeTeamUrl, true);
                var awayTeam = GetAverageGoals(awayTeamUrl, false);

                var homeTeamGoalsExpected = (homeTeam.AverageGoalsScored + awayTeam.AverageGoalsReceived) / 2;
                var awayTeamGoalsExpected = (awayTeam.AverageGoalsScored + homeTeam.AverageGoalsReceived) / 2;

                Console.WriteLine($"Очаква се {homeTeam.Name} да вкарат {homeTeamGoalsExpected.ToString("0.00")} гола.");
                Console.WriteLine($"Очаква се {awayTeam.Name} да вкарат {awayTeamGoalsExpected.ToString("0.00")} гола.");

                var averageGoalsPerGame = GetAverageGoalsPerGame(document);
                var totalGoals = homeTeamGoalsExpected + awayTeamGoalsExpected;

                var expectedGoals = (totalGoals + averageGoalsPerGame) / 2;
                var plusTwoAndAHalf = expectedGoals > 2.5 ? "+" : "-";

                Console.WriteLine();
                Console.WriteLine($"Отборите средно отбелязват {averageGoalsPerGame.ToString("0.00")} гола помежду си.");
                Console.WriteLine($"Очакват се общо {totalGoals.ToString("0.00")} гола.");
                Console.WriteLine($"Математически се очакват общо {expectedGoals.ToString("0.00")} гола.");
                Console.WriteLine($"Препоръчан залог: {plusTwoAndAHalf}2.5");

                Console.WriteLine();
                Console.WriteLine("Натисни Enter за нов мач...");
                Console.ReadLine();
                Console.Clear();
            }
        }

        private static double GetAverageGoalsPerGame(IDocument document)
        {
            var lastGamesBetweenTeams = document.QuerySelectorAll("table.gamesStat")[2].QuerySelector("tbody");
            var resultsAsStrings = GetResultsAsArrayOfStrings(lastGamesBetweenTeams.QuerySelectorAll(".result"));
            var results = resultsAsStrings.Select(x => int.Parse(x[0]) + int.Parse(x[1]));
            var averageGoals = (double)results.Sum() / results.Count();

            return averageGoals;
        }

        private static TeamViewModel GetAverageGoals(string url, bool isHost)
        {
            var teamDocument = browsingContext.OpenAsync(url).Result;
            var teamName = teamDocument.QuerySelector("#teamName2Lines").TextContent;
            var teamTableWithLastResults = teamDocument.QuerySelector("table.gamesStat > tbody");
            var teamGameResults = teamTableWithLastResults.QuerySelectorAll(".result");
            var resultsAsStrings = GetResultsAsArrayOfStrings(teamGameResults);
            var teamGoalsScored = GetGoalsScored(resultsAsStrings, isHost);
            var teamGoalsReceived = GetGoalsReceived(resultsAsStrings, isHost);
            var isHostString = isHost ? "домакини" : "гости";

            Console.WriteLine($"{teamName} вкарват средно по {teamGoalsScored.ToString("0.00")} гола на мач, когато играят като {isHostString}.");
            Console.WriteLine($"{teamName} получават средно по {teamGoalsReceived.ToString("0.00")} гола на мач, когато играят като {isHostString}.");

            return new TeamViewModel()
            {
                Name = teamName,
                AverageGoalsScored = teamGoalsScored,
                AverageGoalsReceived = teamGoalsReceived
            };
        }

        private static IEnumerable<string[]> GetResultsAsArrayOfStrings(IHtmlCollection<IElement> results)
        {
            var resultsAsArrayOfStrings = results.Select(x => x.TextContent.Remove(0, 1).Split('-'));

            return resultsAsArrayOfStrings;
        }

        private static double GetGoalsScored(IEnumerable<string[]> results, bool isHost)
        {
            var isHostValue = isHost ? 0 : 1;
            var totalGoalsScored = results.Select(x => int.Parse(x[isHostValue])).Sum();
            var averageGoalsScored = (double)totalGoalsScored / results.Count();

            return averageGoalsScored;
        }

        private static double GetGoalsReceived(IEnumerable<string[]> results, bool isHost)
        {
            var isHostValue = isHost ? 1 : 0;
            var totalGoalsScored = results.Select(x => int.Parse(x[isHostValue])).Sum();
            var averageGoalsScored = (double)totalGoalsScored / results.Count();

            return averageGoalsScored;
        }
    }
}
