using UnityEngine;
using IBM.Watson.DeveloperCloud.Services.LanguageTranslator.v3;
using IBM.Watson.DeveloperCloud.Services.TextToSpeech.v1;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.Logging;
using System.Collections;
using IBM.Watson.DeveloperCloud.Connection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Text;
using System.IO;
using UnityEditor;
using UnityEngine.Networking;

public class Main : MonoBehaviour
{
    public GameObject environment1;
    public GameObject environment2;
    public GameObject light1;
    public GameObject light2;
    public GameObject tvscreen;
    public GameObject bird1;
    public GameObject bird2;
    public GameObject butterfly;

    public AudioClip[] bgms;
    public AudioSource translationSource;

    KeywordRecognizer keywordRecognizer;
    DictationRecognizer recognizer;
    LanguageTranslator translator;
    TextToSpeech synthesizer;

    List<UniGif.GifTexture> gifTextures;
    string giffyURL;

    int gifFPS;
    int bgmCounter;
    float rotationAngle;
    float rotationSpeed;
    float bflyMoveSpd;


    bool getTranslation;
    bool getSynthesizer;
    bool giffyFinished;
    // Use this for initialization
    void Start()
    {
        recognizer = new DictationRecognizer();
        recognizer.InitialSilenceTimeoutSeconds = 1000.0f;
        recognizer.AutoSilenceTimeoutSeconds = 1000.0f;
        recognizer.DictationResult += DictationResultHandle;
        recognizer.Start();

        getTranslation = false;
        getSynthesizer = false;
        giffyFinished = true;
        gifFPS = 15;
        bgmCounter = 0;
        rotationSpeed = 0f;
        rotationAngle = 0f;

        StartCoroutine(URLTextureToTVScreen("https://i.giphy.com/media/kcog6ebOvhWBW/giphy.gif"));
        StartCoroutine(CreateTextToSpeechService());
        StartCoroutine(CreateSpeechSynthesizerService());

        AudioClip clip = bgms[bgmCounter];
        AudioSource audio = GameObject.Find("BackgroundAudio").GetComponent<AudioSource>();
        audio.clip = clip;
        audio.Play();

        environment1.SetActive(true);
        environment2.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (gifTextures != null)
        {
            int index = (int)(Time.time * gifFPS);
            index = index % gifTextures.Capacity;
            UniGif.GifTexture texture=gifTextures[index];
            tvscreen.GetComponent<Renderer>().material.mainTexture = texture.m_texture2d;
        }
        bird1.transform.rotation = Quaternion.EulerAngles(0, rotationAngle, 0);
        bird2.transform.rotation = Quaternion.EulerAngles(0, rotationAngle, 0);
        rotationAngle += rotationSpeed * Time.deltaTime;
    }

    private IEnumerator Translate(string text)
    {
        translator.GetTranslation(SuccessfulTranslation, FailTranslation, text, "en", "es");
        while (!getTranslation)
            yield return null;
        Debug.Log("Translation completed");
    }

    private IEnumerator TextToSpeech(string text)
    {
        synthesizer.ToSpeech(SuccessfulTextToSpeech, FailTextToSpeech, text, true);
        while (!getSynthesizer)
            yield return null;
        Debug.Log("Synthesizing completed");
    }

    private void DictationResultHandle(string text, ConfidenceLevel confidence)
    {
        Debug.Log(text);
        actionBasedOnDictation(text);
        StartCoroutine(Translate(text));
    }

    private void actionBasedOnDictation(string dictation)
    {
        if (CultureEquals(dictation, new string[] { "mountain" }))
        {
            environment1.SetActive(false);
            environment2.SetActive(true);
        }
        else if (CultureEquals(dictation, new string[] { "room" }))
        {
            environment1.SetActive(true);
            environment2.SetActive(false);
        }
        else if(CultureEquals(dictation, new string[] { "change music", "change", "music"}))
        {
            ChangeBGM();
        }
        else if (CultureEquals(dictation, new string[] { "bright", "writer", "right", "night", "brighter"}))
        {
            light1.GetComponent<Light>().intensity += 0.5f;
            light2.GetComponent<Light>().intensity += 0.5f;
        }
        else if (CultureEquals(dictation, new string[] { "dark", "darker"}))
        {
            light1.GetComponent<Light>().intensity -= 0.5f;
            light2.GetComponent<Light>().intensity -= 0.5f;
        }
        else if (CultureEquals(dictation, new string[] { "turn on tv", "on", "turn on"}))
        {
            tvscreen.SetActive(true);
        }
        else if (CultureEquals(dictation, new string[] { "turn off tv", "off", "turn off" }))
        {
            tvscreen.SetActive(false);
        }
        else if (CultureEquals(dictation, new string[] { "bird rotate up", "rotate up", "locate up"}))
        {
            rotationSpeed += 1f;
        }
        else if (CultureEquals(dictation, new string[] { "bird rotate down", "rotate down", "locate down"}))
        {
            rotationSpeed -= 1f;
        }
        if(giffyFinished)
            StartCoroutine(getGiphyData(dictation));
    }

    private bool CultureEquals(string dictation, string[] matches)
    {
        for(int i = 0; i < matches.Length; i++)
        {
            if (dictation.Equals(matches[i], System.StringComparison.InvariantCultureIgnoreCase))
                return true;
        }
        return false;
    }

    private void SuccessfulTranslation(Translations translation, Dictionary<string, object> customData)
    {
        string jsonStr = customData["json"].ToString();
        int startIdx = jsonStr.IndexOf("translation\":\"") + "translation\":\"".Length;
        int endIdx = jsonStr.IndexOf("\"}],");
        string str2 = jsonStr.Substring(startIdx, endIdx - startIdx);
        byte[] bytes = Encoding.Default.GetBytes(str2);
        string str3 = Encoding.UTF8.GetString(bytes);
        Debug.Log("Langauge Translator - Translate Response: " + str3);
        StreamWriter writer = new StreamWriter(Application.dataPath+"/test.txt", false);
        writer.WriteLine(str3);
        writer.Close();
        getTranslation = true;
        StartCoroutine(TextToSpeech(str3));
    }

    private void FailTranslation(RESTConnector.Error error, Dictionary<string, object> customData)
    {
        Debug.Log("Translation Failed!");
    }

    private IEnumerator CreateTextToSpeechService()
    {
        //  Create credential and instantiate service
        Credentials credentials = null;
        //  Authenticate using iamApikey
        TokenOptions tokenOptions = new TokenOptions()
        {
            IamApiKey = "oZ5rBP6RakY-MbwIADQDAyZUkSAgBwvK-DQaUf7ii7pO",
            IamUrl = "https://iam.bluemix.net/identity/token"
        };

        credentials = new Credentials(tokenOptions, "https://gateway-wdc.watsonplatform.net/language-translator/api");
        while (!credentials.HasIamTokenData())
            yield return null;
        translator =new LanguageTranslator("2018-05-01", credentials);
    }

    private IEnumerator CreateSpeechSynthesizerService()
    {
        Credentials credentials = null;
        //  Authenticate using iamApikey
        TokenOptions tokenOptions = new TokenOptions()
        {
            IamApiKey = "CvJGHUxQHDtg3sFw6OlGgy81HTKb7Uh0K9nbOBjNYNWr",
            IamUrl = "https://iam.bluemix.net/identity/token"
        };
        credentials = new Credentials(tokenOptions, "https://gateway-wdc.watsonplatform.net/text-to-speech/api");
        //  Wait for tokendata
        while (!credentials.HasIamTokenData())
            yield return null;
        synthesizer = new TextToSpeech(credentials);
    }

    private void SuccessfulTextToSpeech(AudioClip clip, Dictionary<string, object> customData = null)
    {
        FileUtil.DeleteFileOrDirectory(Application.dataPath + "/SpanishTranslationVoice");
        SavWav.Save(Application.dataPath + "/SpanishTranslationVoice", clip);
        translationSource.clip = clip;
        translationSource.Play();
        getSynthesizer = true;        
    }

    private void FailTextToSpeech(RESTConnector.Error error, Dictionary<string, object> customData)
    {
        Debug.Log("Text to speech failed!");
    }

    
    private IEnumerator URLTextureToTVScreen(string url)
    {
        using (WWW www = new WWW(url))
        {
            // Wait for download to complete
            yield return www;
            yield return StartCoroutine(UniGif.GetTextureListCoroutine(www.bytes, (gifTexList, loopCount, width, height) =>
            {
                gifTextures = gifTexList;
                giffyFinished = true;
            }));
        }
    }

    private void ChangeBGM()
    {
        bgmCounter++;
        bgmCounter = bgmCounter % 3;
        AudioClip clip = bgms[bgmCounter];
        AudioSource audio = GameObject.Find("BackgroundAudio").GetComponent<AudioSource>();
        audio.clip = clip;
        audio.Play();
    }

    private IEnumerator getGiphyData(string query)
    {
        giffyFinished = false;
        string url = "https://api.giphy.com/v1/gifs/search?api_key=iN2n2ILnWjvtI5ZqOSLzaN68JFZd487v&q=";
        url += query;
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                // Show results as text
                string jsonStr = www.downloadHandler.text;
                int idx=jsonStr.IndexOf("original\":{\"url\":\"")+ "original\":{\"url\":\"".Length;
                string ret = "";
                for(int i = idx; i < jsonStr.Length; i++)
                {
                    if (jsonStr[i] == '"')
                        break;
                    if (jsonStr[i] == '\\')
                        continue;
                    ret += jsonStr[i];
                }
                ret = ret.Substring(14);
                giffyURL = "https://i"+ret;
                Debug.Log(giffyURL);
            }
        }
        StartCoroutine(URLTextureToTVScreen(giffyURL));
    }
}
