## PowershellGPT

A multi-agent workflow to resolve tasks using powershell scripts.

### Workflow overview
![Workflow](asset/image.png)

### Agents overview
- User: accepts user input and send it to manager
- Manager: Create task based on received user question. If the question is non-related to powershell or require more information, it will be sent to customer service for further assistance. Otherwise, it will be sent to enginner for resolution.
- Engineer: Resolve the task using powershell script and send script to powershell agent for execution.
- Powershell: Execute the script. If succeed, the result will be sent directly back to user. Otherwise, it will be sent to engineer for fixing.
- Customer service: Asking user for more information or send the question back to user if it is non-related to powershell.

### Extending the workflow
The workflow can be easily extended to support the following scenarios:
- approve script before execution: Asking user for approval before executing the script.
- support more bash languages: Adding more engineers!

### Example
Below is an example of how the workflow works, the question being asked is "listing all files and its size, and sort by size in descending order".

![Example](asset/output.gif)
