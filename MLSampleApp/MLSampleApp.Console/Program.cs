
using MLSampleApp_Console;

//burada prompt ile gelen verileri PredictiveModel.ModelInput nesnesine atıyoruz. Bu nesne, modelin tahmin yapabilmesi için gerekli olan tüm özellikleri içermelidir.


PredictiveModel.ModelInput input = new PredictiveModel.ModelInput()
{
    Product_ID = @"L47181",
    UDI = 2F,
    Air_temperature = 25.0F,
    Process_temperature = 305.0F,
    Rotational_speed = 1550.0F,
    Torque = 43.0F,
    Tool_wear = 2.0F,
};

Console.WriteLine("Comparing actual Machile Failure with predicted Machine Failure from model...");

// PredictAllLabels metodu, verilen girdi için modelin tahmin ettiği tüm etiketleri döndürür. Bu, modelin birden fazla sınıfı tahmin edebildiği durumlarda kullanışlıdır.

var scoresWithLabel = PredictiveModel.PredictAllLabels(input);

foreach (var score in scoresWithLabel)
{     Console.WriteLine($"Predicted Label: {score.Key}, Score: {score.Value}"); }

