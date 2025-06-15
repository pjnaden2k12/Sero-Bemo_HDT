using DG.Tweening;  
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
public class UIManager : MonoBehaviour
{
    public Button resetPlayPrefs;

    public Button btnUp;
    public Button btnDown;
    public Button btnLeft;
    public Button btnRight;

    public Button[] homeButtons;
    public Button[] resetButtons;
    public Button[] undoBts;
    public Button playBt;
    public Button guideBt;
    public Button exitBt;

    public GameObject level1EffectPanel;

    public GameObject groupMoveBt;
    public GameObject groupSettingBt;
    public GameObject groupLoseBt;
    public GameObject homePanel;
    public GameObject guidePanel;
    public GameObject logo; 
    public event Action<string> OnDirectionButtonPressed;
    public event Action OnPlayPressed;
    public event Action OnResetPressed;
    public event Action OnHomePressed;
    public event Action OnResetPlayPrefsPressed;
    public event Action OnUndoPressed;

    private LevelManager levelManager;
    private CloudScreenEffect cloudEffect;

    public AudioClip buttonClickSound;      
    public AudioClip directionButtonSound;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();

        levelManager = FindFirstObjectByType<LevelManager>();

        btnUp?.onClick.AddListener(() => OnDirectionButtonPressed?.Invoke("Up"));
        btnDown?.onClick.AddListener(() => OnDirectionButtonPressed?.Invoke("Down"));
        btnLeft?.onClick.AddListener(() => OnDirectionButtonPressed?.Invoke("Left"));
        btnRight?.onClick.AddListener(() => OnDirectionButtonPressed?.Invoke("Right"));

        foreach (var btn in homeButtons)
        {
            if (btn != null)
                btn.onClick.AddListener(() => OnHomePressed?.Invoke());
        }

        foreach (var btn in resetButtons)
        {
            if (btn != null)
                btn.onClick.AddListener(() => OnResetPressed?.Invoke());
        }
        foreach (var btn in undoBts)
        {
            if(btn != null)
                btn.onClick.AddListener(()=> OnUndoPressed?.Invoke());
        }
        resetPlayPrefs?.onClick.AddListener(() => OnResetPlayPrefsPressed.Invoke());
        
        playBt?.onClick.AddListener(OnPlayButtonClicked);
        guideBt?.onClick.AddListener(OnPlayGuidePanel);
        exitBt?.onClick.AddListener(ExitGuidePanel);
        ShowHomePanel();
        // Âm thanh cho các nhóm nút thường
        AddSoundToButtons(homeButtons, buttonClickSound);
        AddSoundToButtons(resetButtons, buttonClickSound);
        AddSoundToButtons(undoBts, buttonClickSound);
        AddSoundToButton(playBt, buttonClickSound);
        AddSoundToButton(guideBt, buttonClickSound);
        AddSoundToButton(exitBt, buttonClickSound);

        // Âm thanh riêng cho nút điều hướng
        AddSoundToButton(btnUp, directionButtonSound);
        AddSoundToButton(btnDown, directionButtonSound);
        AddSoundToButton(btnLeft, directionButtonSound);
        AddSoundToButton(btnRight, directionButtonSound);
    }

    private void OnPlayGuidePanel()
    {
        CanvasGroup canvasGroup = guidePanel.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            
            canvasGroup.alpha = 0f;
        }

        guidePanel.SetActive(true); 

        
        canvasGroup?.DOFade(1f, 0.5f);
    }

    private void ExitGuidePanel()
    {
        guidePanel.GetComponent<CanvasGroup>().DOFade(0f, 0.5f).OnComplete(() =>
        {
            guidePanel?.SetActive(false);
        });
    }


    private void OnPlayButtonClicked()
    {
        OnPlayPressed?.Invoke();
        StartCoroutine(DelaySetFalseHome());
    }

    public void ShowHomePanel()
    {
        level1EffectPanel.SetActive(false);
        guideBt?.gameObject.SetActive(false );
        playBt?.gameObject.SetActive(false );
        homePanel?.SetActive(true);
        groupMoveBt?.SetActive(false);
        groupSettingBt?.SetActive(false);
        groupLoseBt?.SetActive(false);

        ResetScale(homePanel);
        ResetScale(groupMoveBt);
        ResetScale(groupSettingBt);
        ResetScale(groupLoseBt);

        StartCoroutine(ShowUIElementsWithDelay()); 
    }

    private IEnumerator ShowUIElementsWithDelay()
    {
        
        logo?.SetActive(true);
        RectTransform logoRect = logo.GetComponent<RectTransform>();
        logoRect.anchoredPosition = new Vector2(logoRect.anchoredPosition.x, 1500);  
        logoRect.DOAnchorPosY(0, 1.5f, false);  
        yield return new WaitForSeconds(1f);  

        
        playBt?.gameObject.SetActive(true);
        playBt.transform.localScale = Vector3.zero;  
        playBt.transform.DOScale(Vector3.one, 0.8f);  
        yield return new WaitForSeconds(0.8f);  

        guideBt?.gameObject.SetActive(true);
        guideBt.transform.localScale = Vector3.zero;  
       guideBt.transform.DOScale(Vector3.one, 0.8f);  
    }

    private void ResetScale(GameObject obj)
    {
        if (obj != null)
            obj.transform.localScale = Vector3.one;
    }

    public void ShowGroupSettingAndMove()
    {
        groupSettingBt?.SetActive(true);
        groupMoveBt?.SetActive(true);

        ResetScale(groupSettingBt);
        ResetScale(groupMoveBt);
    }

    public void HideGroupSettingAndMove()
    {
        groupSettingBt?.SetActive(false);
        groupMoveBt?.SetActive(false);

        ResetScale(groupSettingBt);
        ResetScale(groupMoveBt);
    }

    public void ShowLoseUI()
    {
        if (groupMoveBt != null)
            StartCoroutine(ScaleHide(groupMoveBt));
        if (groupSettingBt != null)
            StartCoroutine(ScaleHide(groupSettingBt));
        if (groupLoseBt != null)
            StartCoroutine(ScaleShow(groupLoseBt));
    }

    private IEnumerator ScaleHide(GameObject target, float duration = 0.3f)
    {
        if (target == null) yield break;

        float timer = 0f;
        while (timer < duration)
        {
            float scale = Mathf.Lerp(1f, 0f, timer / duration);
            target.transform.localScale = new Vector3(scale, scale, scale);
            timer += Time.deltaTime;
            yield return null;
        }

        target.transform.localScale = Vector3.zero;
        target.SetActive(false);
    }

    private IEnumerator ScaleShow(GameObject target, float duration = 0.3f)
    {
        if (target == null) yield break;

        target.SetActive(true);

        float timer = 0f;
        while (timer < duration * 0.7f)
        {
            float scale = Mathf.Lerp(0f, 1.2f, timer / (duration * 0.7f));
            target.transform.localScale = new Vector3(scale, scale, scale);
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0f;
        while (timer < duration * 0.3f)
        {
            float scale = Mathf.Lerp(1.2f, 1f, timer / (duration * 0.3f));
            target.transform.localScale = new Vector3(scale, scale, scale);
            timer += Time.deltaTime;
            yield return null;
        }

        target.transform.localScale = Vector3.one;
    }

    private IEnumerator DelaySetFalseHome()
    {
        yield return new WaitForSeconds(1f);
        homePanel?.SetActive(false);
        groupLoseBt?.SetActive(false);

        if (levelManager != null)
        {
            if (levelManager.IsLevelCompleted() || levelManager.GetCurrentLevel() == 1)
                ShowGroupSettingAndMove();
            else
                HideGroupSettingAndMove();
        }
    }
    private void AddSoundToButtons(Button[] buttons, AudioClip clip)
    {
        foreach (var btn in buttons)
        {
            AddSoundToButton(btn, clip);
        }
    }

    private void AddSoundToButton(Button btn, AudioClip clip)
    {
        if (btn != null)
        {
            btn.onClick.AddListener(() => PlayButtonSound(clip));
        }
    }

    private void PlayButtonSound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    public void ShowLevel1EffectPanelWithEffect()
    {

        if (level1EffectPanel == null) return;

        level1EffectPanel.SetActive(true);

        
        CanvasGroup canvasGroup = level1EffectPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = level1EffectPanel.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;

        canvasGroup.DOFade(0.2f, 0.5f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetId("Level1Blink");
    }



    public void HideLevel1EffectPanel()
    {
        if (level1EffectPanel == null) return;

        DOTween.Kill("Level1Blink"); 

        CanvasGroup canvasGroup = level1EffectPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        level1EffectPanel.transform.localScale = Vector3.one;
        level1EffectPanel.SetActive(false);
    }


}
