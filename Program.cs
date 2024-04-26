// See https://aka.ms/new-console-template for more information

using AutoGen;
using AutoGen.Core;
using Azure.AI.OpenAI;
using Powershell.GPT;

var AZURE_OPENAI_ENDPOINT = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var AZURE_OPENAI_KEY = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
var AZURE_DEPLOYMENT_NAME = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOY_NAME");
var OPENAI_API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var OPENAI_MODEL_ID = Environment.GetEnvironmentVariable("OPENAI_MODEL_ID") ?? "gpt-3.5-turbo-0125";

OpenAIClient client;
bool useAzure = false;
if (AZURE_OPENAI_ENDPOINT is string && AZURE_OPENAI_KEY is string && AZURE_DEPLOYMENT_NAME is string)
{
    client = new OpenAIClient(new Uri(AZURE_OPENAI_ENDPOINT), new Azure.AzureKeyCredential(AZURE_OPENAI_KEY));
    useAzure = true;
}
else if (OPENAI_API_KEY is string)
{
    client = new OpenAIClient(OPENAI_API_KEY);
}
else
{
    throw new ArgumentException("Please provide either (AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_KEY, AZURE_DEPLOYMENT_NAME) or OPENAI_API_KEY");
}

var deployModelName = useAzure ? AZURE_DEPLOYMENT_NAME! : OPENAI_MODEL_ID;
var manager = AgentFactory.CreateManagerAgent(client, modelName: deployModelName);
var pwshDeveloper = AgentFactory.CreatePwshDeveloperAgent(client, Environment.CurrentDirectory, modelName: deployModelName);
var customerService = AgentFactory.CreateCustomerServiceAgent(client, modelName: deployModelName);
var pwshRunner = AgentFactory.CreatePwshRunnerAgent();

var userAgent = new UserProxyAgent("user")
    .RegisterPrintMessage();

// create workflow
var manager2pwshDeveloper = Transition.Create(manager, pwshDeveloper);
var manager2customerService = Transition.Create(manager, customerService);
var pwshDeveloper2pwshRunner = Transition.Create(pwshDeveloper, pwshRunner);
var pwshRunner2pwshDeveloper = Transition.Create(pwshRunner, pwshDeveloper, async (fromAgent, toAgent, msgs) =>
{
    var lastMessage = msgs.Last() ?? throw new ArgumentException("No message to reply to");
    // if last message contains [ERROR], it means the script failed to run because it contains a bug,
    // so we can ask the developer to fix the bug
    if (lastMessage.GetContent()?.Contains("[ERROR]") is true)
    {
        return true;
    }

    return false;
});
var pwshRunner2User = Transition.Create(pwshRunner, userAgent, async (fromAgent, toAgent, msgs) =>
{
    var lastMessage = msgs.Last() ?? throw new ArgumentException("No message to reply to");
    // if last message contains [SUCCEED], it means the script ran successfully,
    // so we can inform the user that the script ran successfully
    if (lastMessage.GetContent()?.Contains("[SUCCESS]") is true)
    {
        return true;
    }

    return false;
});
var customerService2User = Transition.Create(customerService, userAgent);
var user2manager = Transition.Create(userAgent, manager);

var workflow = new Graph([
    manager2pwshDeveloper,
    manager2customerService,
    pwshDeveloper2pwshRunner,
    pwshRunner2pwshDeveloper,
    pwshRunner2User,
    customerService2User,
    user2manager
]);

var groupChatAdmin = AgentFactory.CreateGroupChatAdmin(client);
var groupChat = new GroupChat(
    members: [
        userAgent,
        manager,
        pwshDeveloper,
        customerService,
        pwshRunner,
        ],
    admin: groupChatAdmin,
    workflow: workflow);

// start the chat by asking customer service to create a greeting message
var greetingMessage = await customerService.SendAsync("Create a greeting message for the user and asking them if they have any pwsh task to resolve", ct: default);

// start the chat
await customerService.SendMessageToGroupAsync(groupChat, [greetingMessage], maxRound: 20);