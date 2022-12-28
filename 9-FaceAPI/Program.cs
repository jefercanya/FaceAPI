﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace _9_FaceAPI
{
    class Program
    {
        // From your Face subscription in the Azure portal, get your subscription key and endpoint.
        const string SUBSCRIPTION_KEY =  "PASTE_YOUR_FACE_SUBSCRIPTION_KEY_HERE";
        const string ENDPOINT = "PASTE_YOUR_FACE_ENDPOINT_HERE";
        static string personGroupId = Guid.NewGuid().ToString();

        // Used for all examples.
        // URL for the images.
        const string IMAGE_BASE_URL = "https://csdx.blob.core.windows.net/resources/Face/Images/";

        static void Main(string[] args)
        {
            // Authenticate.
            IFaceClient client = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);

            // Recognition model 4 was released in 2021 February.
            // It is recommended since its accuracy is improved
            // on faces wearing masks compared with model 3,
            // and its overall accuracy is improved compared
            // with models 1 and 2.
            const string RECOGNITION_MODEL4 = RecognitionModel.Recognition04;

            // Detect - get features from faces.
            DetectFaceExtract(client, IMAGE_BASE_URL, RECOGNITION_MODEL4).Wait();
            // Find Similar - find a similar face from a list of faces.
            FindSimilar(client, IMAGE_BASE_URL, RECOGNITION_MODEL4).Wait();
            // Verify - compare two images if the same person or not.
            Verify(client, IMAGE_BASE_URL, RECOGNITION_MODEL4).Wait();

            // Identify - recognize a face(s) in a person group (a person group is created in this example).
            IdentifyInPersonGroup(client, IMAGE_BASE_URL, RECOGNITION_MODEL4).Wait();
            // LargePersonGroup - create, then get data.
            LargePersonGroup(client, IMAGE_BASE_URL, RECOGNITION_MODEL4).Wait();
            // Group faces - automatically group similar faces.
            Group(client, IMAGE_BASE_URL, RECOGNITION_MODEL4).Wait();
            // FaceList - create a face list, then get data
        }

        /*
         *	AUTHENTICATE
         *	Uses subscription key and region to create a client.
         */
        public static IFaceClient Authenticate(string endpoint, string key)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }

        /* 
         * DETECT FACES
         * Detects features from faces and IDs them.
         */
        public static async Task DetectFaceExtract(IFaceClient client, string url, string recognitionModel)
        {
            Console.WriteLine("========DETECT FACES========");
            Console.WriteLine();

            // Create a list of images
            List<string> imageFileNames = new List<string>
                    {
                        "detection1.jpg",    // single female with glasses
                        // "detection2.jpg", // (optional: single man)
                        // "detection3.jpg", // (optional: single male construction worker)
                        // "detection4.jpg", // (optional: 3 people at cafe, 1 is blurred)
                        "detection5.jpg",    // family, woman child man
                        "detection6.jpg"     // elderly couple, male female
                    };

            foreach (var imageFileName in imageFileNames)
            {
                IList<DetectedFace> detectedFaces;

                // Detect faces with all attributes from image url.
                detectedFaces = await client.Face.DetectWithUrlAsync($"{url}{imageFileName}",
                        returnFaceAttributes: new List<FaceAttributeType> { FaceAttributeType.Accessories, FaceAttributeType.Age,
                FaceAttributeType.Blur, FaceAttributeType.Emotion, FaceAttributeType.Exposure, FaceAttributeType.FacialHair,
                FaceAttributeType.Gender, FaceAttributeType.Glasses, FaceAttributeType.Hair, FaceAttributeType.HeadPose,
                FaceAttributeType.Makeup, FaceAttributeType.Noise, FaceAttributeType.Occlusion, FaceAttributeType.Smile },
                        // We specify detection model 1 because we are retrieving attributes.
                        detectionModel: DetectionModel.Detection01,
                        recognitionModel: recognitionModel);

                Console.WriteLine($"{detectedFaces.Count} face(s) detected from image `{imageFileName}`.");

                // Parse and print all attributes of each detected face.
                foreach (var face in detectedFaces)
                {
                    Console.WriteLine($"Face attributes for {imageFileName}:");

                    // Get bounding box of the faces
                    Console.WriteLine($"Rectangle(Left/Top/Width/Height) : {face.FaceRectangle.Left} {face.FaceRectangle.Top} {face.FaceRectangle.Width} {face.FaceRectangle.Height}");

                    // Get accessories of the faces
                    List<Accessory> accessoriesList = (List<Accessory>)face.FaceAttributes.Accessories;
                    int count = face.FaceAttributes.Accessories.Count;
                    string accessory; string[] accessoryArray = new string[count];
                    if (count == 0) { accessory = "NoAccessories"; }
                    else
                    {
                        for (int i = 0; i < count; ++i) { accessoryArray[i] = accessoriesList[i].Type.ToString(); }
                        accessory = string.Join(",", accessoryArray);
                    }
                    Console.WriteLine($"Accessories : {accessory}");

                    // Get face other attributes
                    Console.WriteLine($"Age : {face.FaceAttributes.Age}");
                    Console.WriteLine($"Blur : {face.FaceAttributes.Blur.BlurLevel}");

                    // Get emotion on the face
                    string emotionType = string.Empty;
                    double emotionValue = 0.0;
                    Emotion emotion = face.FaceAttributes.Emotion;
                    if (emotion.Anger > emotionValue) { emotionValue = emotion.Anger; emotionType = "Anger"; }
                    if (emotion.Contempt > emotionValue) { emotionValue = emotion.Contempt; emotionType = "Contempt"; }
                    if (emotion.Disgust > emotionValue) { emotionValue = emotion.Disgust; emotionType = "Disgust"; }
                    if (emotion.Fear > emotionValue) { emotionValue = emotion.Fear; emotionType = "Fear"; }
                    if (emotion.Happiness > emotionValue) { emotionValue = emotion.Happiness; emotionType = "Happiness"; }
                    if (emotion.Neutral > emotionValue) { emotionValue = emotion.Neutral; emotionType = "Neutral"; }
                    if (emotion.Sadness > emotionValue) { emotionValue = emotion.Sadness; emotionType = "Sadness"; }
                    if (emotion.Surprise > emotionValue) { emotionType = "Surprise"; }
                    Console.WriteLine($"Emotion : {emotionType}");

                    // Get more face attributes
                    Console.WriteLine($"Exposure : {face.FaceAttributes.Exposure.ExposureLevel}");
                    Console.WriteLine($"FacialHair : {string.Format("{0}", face.FaceAttributes.FacialHair.Moustache + face.FaceAttributes.FacialHair.Beard + face.FaceAttributes.FacialHair.Sideburns > 0 ? "Yes" : "No")}");
                    Console.WriteLine($"Gender : {face.FaceAttributes.Gender}");
                    Console.WriteLine($"Glasses : {face.FaceAttributes.Glasses}");

                    // Get hair color
                    Hair hair = face.FaceAttributes.Hair;
                    string color = null;
                    if (hair.HairColor.Count == 0) { if (hair.Invisible) { color = "Invisible"; } else { color = "Bald"; } }
                    HairColorType returnColor = HairColorType.Unknown;
                    double maxConfidence = 0.0f;
                    foreach (HairColor hairColor in hair.HairColor)
                    {
                        if (hairColor.Confidence <= maxConfidence) { continue; }
                        maxConfidence = hairColor.Confidence; returnColor = hairColor.Color; color = returnColor.ToString();
                    }
                    Console.WriteLine($"Hair : {color}");

                    // Get more attributes
                    Console.WriteLine($"HeadPose : {string.Format("Pitch: {0}, Roll: {1}, Yaw: {2}", Math.Round(face.FaceAttributes.HeadPose.Pitch, 2), Math.Round(face.FaceAttributes.HeadPose.Roll, 2), Math.Round(face.FaceAttributes.HeadPose.Yaw, 2))}");
                    Console.WriteLine($"Makeup : {string.Format("{0}", (face.FaceAttributes.Makeup.EyeMakeup || face.FaceAttributes.Makeup.LipMakeup) ? "Yes" : "No")}");
                    Console.WriteLine($"Noise : {face.FaceAttributes.Noise.NoiseLevel}");
                    Console.WriteLine($"Occlusion : {string.Format("EyeOccluded: {0}", face.FaceAttributes.Occlusion.EyeOccluded ? "Yes" : "No")} " +
                        $" {string.Format("ForeheadOccluded: {0}", face.FaceAttributes.Occlusion.ForeheadOccluded ? "Yes" : "No")}   {string.Format("MouthOccluded: {0}", face.FaceAttributes.Occlusion.MouthOccluded ? "Yes" : "No")}");
                    Console.WriteLine($"Smile : {face.FaceAttributes.Smile}");
                    Console.WriteLine();
                }
            }

        }

        private static async Task<List<DetectedFace>> DetectFaceRecognize(IFaceClient faceClient, string url, string recognition_model)
        {
            // Detect faces from image URL. Since only recognizing, use the recognition model 1.
            // We use detection model 3 because we are not retrieving attributes.
            IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithUrlAsync(url, recognitionModel: recognition_model, detectionModel: DetectionModel.Detection03);
            Console.WriteLine($"{detectedFaces.Count} face(s) detected from image `{Path.GetFileName(url)}`");
            return detectedFaces.ToList();
        }

        /*
         * FIND SIMILAR
         * This example will take an image and find a similar one to it in another image.
         */
        public static async Task FindSimilar(IFaceClient client, string url, string recognition_model)
        {
            Console.WriteLine("========FIND SIMILAR========");
            Console.WriteLine();

            List<string> targetImageFileNames = new List<string>
                        {
                            "Family1-Dad1.jpg",
                            "Family1-Daughter1.jpg",
                            "Family1-Mom1.jpg",
                            "Family1-Son1.jpg",
                            "Family2-Lady1.jpg",
                            "Family2-Man1.jpg",
                            "Family3-Lady1.jpg",
                            "Family3-Man1.jpg"
                        };

            string sourceImageFileName = "findsimilar.jpg";
            IList<Guid?> targetFaceIds = new List<Guid?>();
            foreach (var targetImageFileName in targetImageFileNames)
            {
                // Detect faces from target image url.
                var faces = await DetectFaceRecognize(client, $"{url}{targetImageFileName}", recognition_model);
                // Add detected faceId to list of GUIDs.
                targetFaceIds.Add(faces[0].FaceId.Value);
            }

            // Detect faces from source image url.
            IList<DetectedFace> detectedFaces = await DetectFaceRecognize(client, $"{url}{sourceImageFileName}", recognition_model);
            Console.WriteLine();

            // Find a similar face(s) in the list of IDs. Comapring only the first in list for testing purposes.
            IList<SimilarFace> similarResults = await client.Face.FindSimilarAsync(detectedFaces[0].FaceId.Value, null, null, targetFaceIds);

            foreach (var similarResult in similarResults)
            {
                Console.WriteLine($"Faces from {sourceImageFileName} & ID:{similarResult.FaceId} are similar with confidence: {similarResult.Confidence}.");
            }
            Console.WriteLine();

            
        }

        public static async Task IdentifyInPersonGroup(IFaceClient client, string url, string recognitionModel)
        {
            Console.WriteLine("========IDENTIFY FACES========");
            Console.WriteLine();

            // Create a dictionary for all your images, grouping similar ones under the same key.
            Dictionary<string, string[]> personDictionary =
                new Dictionary<string, string[]>
                    { { "Family1-Dad", new[] { "Family1-Dad1.jpg", "Family1-Dad2.jpg" } },
              { "Family1-Mom", new[] { "Family1-Mom1.jpg", "Family1-Mom2.jpg" } },
              { "Family1-Son", new[] { "Family1-Son1.jpg", "Family1-Son2.jpg" } },
              { "Family1-Daughter", new[] { "Family1-Daughter1.jpg", "Family1-Daughter2.jpg" } },
              { "Family2-Lady", new[] { "Family2-Lady1.jpg", "Family2-Lady2.jpg" } },
              { "Family2-Man", new[] { "Family2-Man1.jpg", "Family2-Man2.jpg" } }
                    };
            // A group photo that includes some of the persons you seek to identify from your dictionary.
            string sourceImageFileName = "identification1.jpg";

            // Create a person group. 
            Console.WriteLine($"Create a person group ({personGroupId}).");
            await client.PersonGroup.CreateAsync(personGroupId, personGroupId, recognitionModel: recognitionModel);
            // The similar faces will be grouped into a single person group person.
            foreach (var groupedFace in personDictionary.Keys)
            {
                // Limit TPS
                await Task.Delay(250);
                Person person = await client.PersonGroupPerson.CreateAsync(personGroupId: personGroupId, name: groupedFace);
                Console.WriteLine($"Create a person group person '{groupedFace}'.");

                // Add face to the person group person.
                foreach (var similarImage in personDictionary[groupedFace])
                {
                    Console.WriteLine($"Add face to the person group person({groupedFace}) from image `{similarImage}`");
                    PersistedFace face = await client.PersonGroupPerson.AddFaceFromUrlAsync(personGroupId, person.PersonId,
                        $"{url}{similarImage}", similarImage);
                }
            }

            // Start to train the person group.
            Console.WriteLine();
            Console.WriteLine($"Train person group {personGroupId}.");
            await client.PersonGroup.TrainAsync(personGroupId);

            // Wait until the training is completed.
            while (true)
            {
                await Task.Delay(1000);
                var trainingStatus = await client.PersonGroup.GetTrainingStatusAsync(personGroupId);
                Console.WriteLine($"Training status: {trainingStatus.Status}.");
                if (trainingStatus.Status == TrainingStatusType.Succeeded) { break; }
            }
            Console.WriteLine();

            List<Guid> sourceFaceIds = new List<Guid>();
            // Detect faces from source image url.
            List<DetectedFace> detectedFaces = await DetectFaceRecognize(client, $"{url}{sourceImageFileName}", recognitionModel);

            // Add detected faceId to sourceFaceIds.
            foreach (var detectedFace in detectedFaces) { sourceFaceIds.Add(detectedFace.FaceId.Value); }

            // Identify the faces in a person group. 
            var identifyResults = await client.Face.IdentifyAsync(sourceFaceIds, personGroupId);

            foreach (var identifyResult in identifyResults)
            {
                Person person = await client.PersonGroupPerson.GetAsync(personGroupId, identifyResult.Candidates[0].PersonId);
                Console.WriteLine($"Person '{person.Name}' is identified for face in: {sourceImageFileName} - {identifyResult.FaceId}," +
                    $" confidence: {identifyResult.Candidates[0].Confidence}.");
            }
            Console.WriteLine();

            // At end, delete person groups in both regions (since testing only)
            Console.WriteLine("========DELETE PERSON GROUP========");
            Console.WriteLine();
            DeletePersonGroup(client, personGroupId).Wait();
        }

        /*
		 * LARGE PERSON GROUP
		 * The example will create a large person group, retrieve information from it, 
		 * list the Person IDs it contains, and finally delete a large person group.
		 * For simplicity, the same images are used for the regular-sized person group in IDENTIFY FACES of this quickstart.
		 * A large person group is made up of person group persons. 
		 * One person group person is made up of many similar images of that person, which are each PersistedFace objects.
		 */
        public static async Task LargePersonGroup(IFaceClient client, string url, string recognitionModel)
        {
            Console.WriteLine("========LARGE PERSON GROUP========");
            Console.WriteLine();

            // Create a dictionary for all your images, grouping similar ones under the same key.
            Dictionary<string, string[]> personDictionary =
            new Dictionary<string, string[]>
                { { "Family1-Dad", new[] { "Family1-Dad1.jpg", "Family1-Dad2.jpg" } },
                      { "Family1-Mom", new[] { "Family1-Mom1.jpg", "Family1-Mom2.jpg" } },
                      { "Family1-Son", new[] { "Family1-Son1.jpg", "Family1-Son2.jpg" } },
                      { "Family1-Daughter", new[] { "Family1-Daughter1.jpg", "Family1-Daughter2.jpg" } },
                      { "Family2-Lady", new[] { "Family2-Lady1.jpg", "Family2-Lady2.jpg" } },
                      { "Family2-Man", new[] { "Family2-Man1.jpg", "Family2-Man2.jpg" } }
                };

            // Create a large person group ID. 
            string largePersonGroupId = Guid.NewGuid().ToString();
            Console.WriteLine($"Create a large person group ({largePersonGroupId}).");

            // Create the large person group
            await client.LargePersonGroup.CreateAsync(largePersonGroupId: largePersonGroupId, name: largePersonGroupId, recognitionModel);

            // Create Person objects from images in our dictionary
            // We'll store their IDs in the process
            List<Guid> personIds = new List<Guid>();
            foreach (var groupedFace in personDictionary.Keys)
            {
                // Limit TPS
                await Task.Delay(250);

                Person personLarge = await client.LargePersonGroupPerson.CreateAsync(largePersonGroupId, groupedFace);
                Console.WriteLine();
                Console.WriteLine($"Create a large person group person '{groupedFace}' ({personLarge.PersonId}).");

                // Store these IDs for later retrieval
                personIds.Add(personLarge.PersonId);

                // Add face to the large person group person.
                foreach (var image in personDictionary[groupedFace])
                {
                    Console.WriteLine($"Add face to the person group person '{groupedFace}' from image `{image}`");
                    PersistedFace face = await client.LargePersonGroupPerson.AddFaceFromUrlAsync(largePersonGroupId, personLarge.PersonId,
                        $"{url}{image}", image);
                }
            }

            // Start to train the large person group.
            Console.WriteLine();
            Console.WriteLine($"Train large person group {largePersonGroupId}.");
            await client.LargePersonGroup.TrainAsync(largePersonGroupId);

            // Wait until the training is completed.
            while (true)
            {
                await Task.Delay(1000);
                var trainingStatus = await client.LargePersonGroup.GetTrainingStatusAsync(largePersonGroupId);
                Console.WriteLine($"Training status: {trainingStatus.Status}.");
                if (trainingStatus.Status == TrainingStatusType.Succeeded) { break; }
            }
            Console.WriteLine();

            // Now that we have created and trained a large person group, we can retrieve data from it.
            // Get list of persons and retrieve data, starting at the first Person ID in previously saved list.
            IList<Person> persons = await client.LargePersonGroupPerson.ListAsync(largePersonGroupId, start: "");

            Console.WriteLine($"Persisted Face IDs (from {persons.Count} large person group persons): ");
            foreach (Person person in persons)
            {
                foreach (Guid pFaceId in person.PersistedFaceIds)
                {
                    Console.WriteLine($"The person '{person.Name}' has an image with ID: {pFaceId}");
                }
            }
            Console.WriteLine();

            // After testing, delete the large person group, PersonGroupPersons also get deleted.
            await client.LargePersonGroup.DeleteAsync(largePersonGroupId);
            Console.WriteLine($"Deleted the large person group {largePersonGroupId}.");
            Console.WriteLine();
        }
        /*
		 * END - LARGE PERSON GROUP
		 */



        /*
       * VERIFY
       * The Verify operation takes a face ID from DetectedFace or PersistedFace and either another face ID 
       * or a Person object and determines whether they belong to the same person. If you pass in a Person object, 
       * you can optionally pass in a PersonGroup to which that Person belongs to improve performance.
       */
        public static async Task Verify(IFaceClient client, string url, string recognitionModel03)
        {
            Console.WriteLine("========VERIFY========");
            Console.WriteLine();

            List<string> targetImageFileNames = new List<string> { "Family1-Dad1.jpg", "Family1-Dad2.jpg" };
            string sourceImageFileName1 = "Family1-Dad3.jpg";
            string sourceImageFileName2 = "Family1-Son1.jpg";


            List<Guid> targetFaceIds = new List<Guid>();
            foreach (var imageFileName in targetImageFileNames)
            {
                // Detect faces from target image url.
                List<DetectedFace> detectedFaces = await DetectFaceRecognize(client, $"{url}{imageFileName} ", recognitionModel03);
                targetFaceIds.Add(detectedFaces[0].FaceId.Value);
                Console.WriteLine($"{detectedFaces.Count} faces detected from image `{imageFileName}`.");
            }

            // Detect faces from source image file 1.
            List<DetectedFace> detectedFaces1 = await DetectFaceRecognize(client, $"{url}{sourceImageFileName1} ", recognitionModel03);
            Console.WriteLine($"{detectedFaces1.Count} faces detected from image `{sourceImageFileName1}`.");
            Guid sourceFaceId1 = detectedFaces1[0].FaceId.Value;

            // Detect faces from source image file 2.
            List<DetectedFace> detectedFaces2 = await DetectFaceRecognize(client, $"{url}{sourceImageFileName2} ", recognitionModel03);
            Console.WriteLine($"{detectedFaces2.Count} faces detected from image `{sourceImageFileName2}`.");
            Guid sourceFaceId2 = detectedFaces2[0].FaceId.Value;

            // Verification example for faces of the same person.
            VerifyResult verifyResult1 = await client.Face.VerifyFaceToFaceAsync(sourceFaceId1, targetFaceIds[0]);
            Console.WriteLine(
                verifyResult1.IsIdentical
                    ? $"Faces from {sourceImageFileName1} & {targetImageFileNames[0]} are of the same (Positive) person, similarity confidence: {verifyResult1.Confidence}."
                    : $"Faces from {sourceImageFileName1} & {targetImageFileNames[0]} are of different (Negative) persons, similarity confidence: {verifyResult1.Confidence}.");

            // Verification example for faces of different persons.
            VerifyResult verifyResult2 = await client.Face.VerifyFaceToFaceAsync(sourceFaceId2, targetFaceIds[0]);
            Console.WriteLine(
                verifyResult2.IsIdentical
                    ? $"Faces from {sourceImageFileName2} & {targetImageFileNames[0]} are of the same (Negative) person, similarity confidence: {verifyResult2.Confidence}."
                    : $"Faces from {sourceImageFileName2} & {targetImageFileNames[0]} are of different (Positive) persons, similarity confidence: {verifyResult2.Confidence}.");

            Console.WriteLine();
        }
        /*
		 * END - VERIFY 
		 */

        /*
        * GROUP FACES
        * This method of grouping is useful if you don't need to create a person group. It will automatically group similar
        * images, whereas the person group method allows you to define the grouping.
        * A single "messyGroup" array contains face IDs for which no similarities were found.
        */
        public static async Task Group(IFaceClient client, string url, string recognition_model)
        {
            Console.WriteLine("========GROUP FACES========");
            Console.WriteLine();

            // Create list of image names
            List<string> imageFileNames = new List<string>
                              {
                                  "Family1-Dad1.jpg",
                                  "Family1-Dad2.jpg",
                                  "Family3-Lady1.jpg",
                                  "Family1-Daughter1.jpg",
                                  "Family1-Daughter2.jpg",
                                  "Family1-Daughter3.jpg"
                              };
            // Create empty dictionary to store the groups
            Dictionary<string, string> faces = new Dictionary<string, string>();
            List<Guid> faceIds = new List<Guid>();

            // First, detect the faces in your images
            foreach (var imageFileName in imageFileNames)
            {
                // Detect faces from image url.
                IList<DetectedFace> detectedFaces = await DetectFaceRecognize(client, $"{url}{imageFileName}", recognition_model);
                // Add detected faceId to faceIds and faces.
                faceIds.Add(detectedFaces[0].FaceId.Value);
                faces.Add(detectedFaces[0].FaceId.ToString(), imageFileName);
            }
            Console.WriteLine();
            // Group the faces. Grouping result is a group collection, each group contains similar faces.
            var groupResult = await client.Face.GroupAsync(faceIds);

            // Face groups contain faces that are similar to all members of its group.
            for (int i = 0; i < groupResult.Groups.Count; i++)
            {
                Console.Write($"Found face group {i + 1}: ");
                foreach (var faceId in groupResult.Groups[i]) { Console.Write($"{faces[faceId.ToString()]} "); }
                Console.WriteLine(".");
            }

            // MessyGroup contains all faces which are not similar to any other faces. The faces that cannot be grouped.
            if (groupResult.MessyGroup.Count > 0)
            {
                Console.Write("Found messy face group: ");
                foreach (var faceId in groupResult.MessyGroup) { Console.Write($"{faces[faceId.ToString()]} "); }
                Console.WriteLine(".");
            }
            Console.WriteLine();
        }
        /*
		 * END - GROUP FACES
		 */


        /*
         * DELETE PERSON GROUP
         * After this entire example is executed, delete the person group in your Azure account,
         * otherwise you cannot recreate one with the same name (if running example repeatedly).
         */
        public static async Task DeletePersonGroup(IFaceClient client, String personGroupId)
        {
            await client.PersonGroup.DeleteAsync(personGroupId);
            Console.WriteLine($"Deleted the person group {personGroupId}.");
        }
    }
}

