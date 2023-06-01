### About
This project forwards microsoft message queuing to aws simple queue service and vice-versa.
### Requirements
It runs on windows using .net 4.7
### Getting started
- Download [msmq-to-sqs.zip](https://github.com/jcardus/msmq-to-sqs/releases/download/1.2/Archive.zip)
- Extract contents to the desired folder
- Replace the msmq-to-sqs.exe.config content with correct configuration
- Install the windows service by opening a command prompt **as an Administrator** in the extracted folder and running the following command:
`\windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe msmq-to-sqs.exe`
- You should see the following message:
> The Commit phase completed successfully.
>
> The transacted install has completed.



- Project [msmq-to-sqs](https://github.com/jcardus/msmq-to-sqs)
