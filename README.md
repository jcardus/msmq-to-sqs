### About
This project forwards microsoft message queuing to aws simple queue service and vice-versa.
### Requirements
It runs on windows using .net 4.7
### Getting started
- Download latest zip file from [releases](https://github.com/jcardus/msmq-to-sqs/releases)
- Extract contents to the desired folder
- Replace the msmq-to-sqs.exe.config context with correct configuration
- Install the windows service by opening a command prompt **as an Administrator** and running the following command in the extracted folder:
`\windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe msmq-to-sqs.exe`
- You should see the following message:
> The Commit phase completed successfully.
>
> The transacted install has completed.
