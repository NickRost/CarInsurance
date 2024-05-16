using Telegram.Bot.Polling;
using Telegram.Bot;
using Telegram.Bot.Types;
using Mindee;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Mindee.Input;
using Mindee.Product.Passport;
using System.Text;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using OpenAI_API;
using System;

internal class Program
{
    private static ITelegramBotClient _botClient;

    private static ReceiverOptions _receiverOptions;

    private static void Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json");

        var config = configuration.Build();

        TelegramBotClient? botClient = new TelegramBotClient(config["TelegramAPIKey"]);

        botClient.StartReceiving(UpdateHandler, ErrorHandler);
        Console.WriteLine("Bot started!");
        Console.ReadKey();
    }

    public static async Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken token)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json");

        var config = configuration.Build();

        OpenAIAPI api = new OpenAIAPI(new APIAuthentication(config["OpenAPIKey"]));

        if (update.Message?.Text == "/start") // introduction
        {
            var chat = api.Chat.CreateConversation();
            chat.AppendSystemMessage("You are a a Insurrance Company helper bot. You have to introduce itself and explain that its purpose is to assist with car insurance purchases. After that you have to request user to send photos of his passport and vehicle id document");
            chat.AppendUserInput("Introduce yourself and ask me to send you photos of his passport and vehicle id document");
            string response = await chat.GetResponseFromChatbotAsync();
            await bot.SendTextMessageAsync(update.Message.Chat.Id, response);
        }
        else if (update.Message?.Photo != null) // getting a photo and pulling details from Mindee
        {
            await bot.SendTextMessageAsync(update.Message.Chat.Id, "Photo is received");

            var fileId = update.Message.Photo[3].FileId;

            var result = await bot.GetFileAsync(fileId);

            MindeeClient mindeeClient = new MindeeClient(config["MindeeAPIKey"]);
            UrlInputSource urlInputSource = new UrlInputSource("https://api.telegram.org/file/bot6566911207:AAEVxxYfVJEjWaZC0GqCy68rpt5ynCxMvIw/" + result.FilePath);

            var response = await mindeeClient.ParseAsync<PassportV1>(urlInputSource);
            Console.WriteLine(response.Document.Inference.Prediction.ToString());

            await bot.SendTextMessageAsync(update.Message.Chat.Id, "Here is your data:\n" + response.Document.Inference.Prediction.ToString());
            StringBuilder sb = new StringBuilder();
            await bot.SendTextMessageAsync(update.Message.Chat.Id, "Please confirm if the data is alright", null, ParseMode.Html, replyMarkup: ConfirmPhotoBuilder());


        } 
        else if (update.CallbackQuery?.Data == "1") // Details Confirmation
        {
            var chat = api.Chat.CreateConversation();
            chat.AppendSystemMessage("You are a a Insurrance Company consultant. You have to thank user for photos of his passport for verification");
            chat.AppendUserInput("I have sent photos of my documents");
            string response = await chat.GetResponseFromChatbotAsync();
            await bot.SendTextMessageAsync(update.CallbackQuery.From.Id, response);
            await bot.DeleteMessageAsync(update.CallbackQuery?.From.Id, (int)(update.CallbackQuery?.Message?.MessageId));

            chat = api.Chat.CreateConversation();
            chat.AppendSystemMessage("You are a a Insurrance Company consultant. Now you have to tell user that price for this insurance is 100USD and explain the price");
            chat.AppendUserInput("What is the price?");
            response = await chat.GetResponseFromChatbotAsync();

            await bot.SendTextMessageAsync(update.CallbackQuery.From.Id, response, null, ParseMode.Html, replyMarkup: ConfirmPriceBuilder());

        } 
        else if (update.CallbackQuery?.Data == "priceCofirmed") // Price Confirmation
        {
            await bot.DeleteMessageAsync(update.CallbackQuery?.From.Id, (int)(update.CallbackQuery?.Message?.MessageId));

            var chat = api.Chat.CreateConversation();
            chat.AppendSystemMessage("You are a a Insurrance Company consultant bot. You have to thank user for the price confirmation from his side");
            chat.AppendUserInput("I agree with this price");
            string response = await chat.GetResponseFromChatbotAsync();
            await bot.SendTextMessageAsync(update.CallbackQuery.From.Id, response);

            chat = api.Chat.CreateConversation();
            chat.AppendSystemMessage("You are a a Insurrance Company consultant bot. You have to generate a purchase confirmation document with this template: Of course! Here is your purchase confirmation document:\r\n\r\nPurchase Confirmation\r\n\r\nCustomer Name: [Customer Name]\r\nPolicy Number: [Policy Number]\r\nInsurance Type: [Insurance Type]\r\nCoverage Start Date: [Start Date]\r\nCoverage End Date: [End Date]\r\nPremium Amount: [Amount]\r\nPayment Method: [Payment Method]\r\n\r\nThank you for choosing our insurance services. If you have any questions or need further assistance, please do not hesitate to contact us.\r\n\r\nSincerely,\r\n[Insurance Company Name]");
            chat.AppendUserInput("Can I get my confirmation document?");
            response = await chat.GetResponseFromChatbotAsync(); // For storing users data to put in purchase documents I would use some kind of Azure Cosmos DB or Azure Table storage.

            await bot.SendTextMessageAsync(update.CallbackQuery.From.Id, response);
        }
        else if (update.CallbackQuery?.Data == "0") // decline details from Mindee
        {
            var chat = api.Chat.CreateConversation();
            chat.AppendSystemMessage("You are a a Insurrance Company consultant. You have to request user to send photos of his passport and vehicle id document");
            chat.AppendUserInput("What do I do now?");
            string response = await chat.GetResponseFromChatbotAsync();

            await bot.SendTextMessageAsync(update.CallbackQuery.From.Id, response);
        } else
        {
            var chat = api.Chat.CreateConversation();
            chat.AppendSystemMessage("You are a a Insurrance Company consultant. You have to request user to send photos of his passport and vehicle id document");
            chat.AppendUserInput("What do I do now?");
            string response = await chat.GetResponseFromChatbotAsync();

            await bot.SendTextMessageAsync(update.Message?.Chat.Id, response);
        }
    }
    public static async Task ErrorHandler(ITelegramBotClient bot, Exception exception, CancellationToken token) 
    {
        await Console.Out.WriteLineAsync(exception.Message); // display error in console, but actually it is much better to use Azure app insights in prod environment
    }

    private static InlineKeyboardMarkup ConfirmPhotoBuilder()
    {
        StringBuilder sb = new StringBuilder();

        // Buttons
        InlineKeyboardButton urlButton = new InlineKeyboardButton("Agree");
        InlineKeyboardButton urlButton2 = new InlineKeyboardButton("Decline");

        urlButton.Text = "Agree";
        urlButton.CallbackData = "1";

        urlButton2.Text = "Decline";
        urlButton2.CallbackData = "0";



        InlineKeyboardButton[] buttons = new InlineKeyboardButton[] { urlButton, urlButton2 };

        // Keyboard markup
        InlineKeyboardMarkup inline = new InlineKeyboardMarkup(buttons);

        return inline;
    }

    private static InlineKeyboardMarkup ConfirmPriceBuilder()
    {
        StringBuilder sb = new StringBuilder();

        // Buttons
        InlineKeyboardButton urlButton = new InlineKeyboardButton("Agree with price");

        urlButton.Text = "Agree";
        urlButton.CallbackData = "priceCofirmed";



        InlineKeyboardButton[] buttons = new InlineKeyboardButton[] { urlButton };

        // Keyboard markup
        InlineKeyboardMarkup inline = new InlineKeyboardMarkup(buttons);

        return inline;
    }
}