using AutoGen.Core;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using Azure.AI.OpenAI;

namespace Powershell.GPT;

internal static class AgentFactory
{
    public static IAgent CreateManagerAgent(OpenAIClient client, string name = "Manager", string modelName = "gpt-35-turbo-0125")
    {
        var planner = new OpenAIChatAgent(
            openAIClient: client,
            name: name,
            modelName: modelName,
            systemMessage: """
            You are a manager of an software development team. You need to convert user's question to a task and assign it to one of your team member.

            here's your team member:
            - powershell developer: A developer who is expert in powershell
            - customer service: A customer service representative who can talk to the customer and get the requirements

            The task you create should be a json object with the following properties:
            - name: The name of the task
            - description: A description of the task
            - to: The member who you want to assign the task to

            here are some examples of tasks:
            {
                "name": "Shut down the server",
                "description": "Create a powershell script to shut down the server",
                "to": "powershell developer"
            }
            {
                "name": "Need more information"
                "description": "Talk to the customer and get the requirements for the new feature",
                "to": "customer service"
            }
            {
                "name": "Not A Task"
                "description": "Tell customer that their question is not relevant to powershell",
                "to": "customer service"
            }
            """,
            responseFormat: ChatCompletionsResponseFormat.JsonObject)
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        return planner;
    }

    public static IAgent CreatePwshDeveloperAgent(
        OpenAIClient client,
        string cwd,
        string name = "powershell developer",
        string modelName = "gpt-35-turbo-0125")
    {
        var agent = new OpenAIChatAgent(
            openAIClient: client,
            modelName: modelName,
            name: name,
            systemMessage: $"""
            You are a powershell developer. You need to convert the task assigned to you to a powershell script.
            
            If there is bug in the script, you need to fix it.

            The current working directory is {cwd}

            You need to write powershell script to resolve task. Put the script between ```pwsh and ```.
            The script should always write the result to the output stream using Write-Host command.

            e.g.
            ```pwsh
            # This is a powershell script
            Write-Host "Hello, World!"
            ```
            """)
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        return agent;
    }

    public static IAgent CreateCustomerServiceAgent(OpenAIClient client, string name = "customer service", string modelName = "gpt-35-turbo-0125")
    {
        var agent = new OpenAIChatAgent(
            openAIClient: client,
            modelName: modelName,
            name: name,
            systemMessage: """
            You are a customer service representative. You need to convert the task assigned to you to a polite yet clear question that you can ask the customer to get the answer.
            """)
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        return agent;
    }

    public static IAgent CreateGroupChatAdmin(OpenAIClient client, string name = "group chat admin", string modelName = "gpt-35-turbo-0125")
    {
        var agent = new OpenAIChatAgent(
            openAIClient: client,
            modelName: modelName,
            name: name)
            .RegisterMessageConnector();

        return agent;
    }

    public static IAgent CreatePwshRunnerAgent(string name = "powershell runner")
    {
        var agent = new PowershellRunnerAgent(name)
            .RegisterPrintMessage();

        return agent;
    }
}
