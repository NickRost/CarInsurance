It is a Telegram Bot for CarInsurance Sales.

https://t.me/CarInsuranceSalesBot

Setup is pretty straightforward. All you need is clone this project and build and run. 
Bot workflow is pretty clear too. You can see comments in the code to see how it works.
Interaction flow with bot will be shown on video.

My comments: I would use Azure Cosmos DB or table storage to temporary store the data for creating dummy purchase documents. Also, keeping these API codes in appsettings is a bad practice. I would use Azure KeyVault for this. 
I didn't use all this because even though there is no deadline, but I didn't want to spend too much time on this assessment and keep it simple.  
Tha app is currently deployed on Azure Web App Web Job.

Here is a video link https://drive.google.com/file/d/1LSBBsXexUALUV91i9yduS-NYXAqAqhVD/view?usp=sharing
