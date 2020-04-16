﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {
    //单例
    private static GameManager _instance;
    public static GameManager instance {
        get { return _instance; }
        set { _instance = value; }
    }
    public int xCol;//列
    public int yRow;//行
    public GameObject gridPrefab;
    public ModelBase lastSelectModel;
    public ModelBase selectModel;//鼠标点击的当前对象
    public ModelBase targetModel;//玩家目的移动对象
    //种类
    public enum ModelType {
        //空,默认,障碍物,行消除,列消除,彩虹道具
        Empty, Normal, Wall, CrossClear, RainBow, Count//count为标记类型
    }
    //通过字典查找对应类型的预制体
    public Dictionary<ModelType, GameObject> modelPrefabDict;
    [System.Serializable]
    public struct ModelPrefab {
        public ModelType type;
        public GameObject prefab;
    }
    //结构体数组
    public ModelPrefab[] modelPrefabs;
    public ModelBase[,] models;//二维数组
    public float fillTime = 0.1f;//填充时间间隔
    //audio
    public AudioClip[] audios;
    //UI控制相关
    public GameObject pausePanel;
    public GameObject aboutUs;
    public GameObject audioOnOff;
    public bool canAudio;
    public Text scoreText;//score
    public Text restTimeText;//time
    public float gameTime = 60f;
    public bool gameover;
    public int score;
    public GameObject gameoverPanel;
    public Text overScoreText;
    public Text historySoreText;
    public GameObject breakRecord;
    public Transform spawn;
    public GameObject[] excellent;
    public GameObject beginPanel;
    public float time = 0;
    public bool gameBegin;
    private int scoreStep;

    public Transform canvas;
    public GameObject[] timeAddEffetc;
    public int awardNums;
    public Text curStep;
    //二次移动
    public int step;
    void Awake() {
        step = 0;
        //Destroy(this);
        awardNums = 0;
        gameBegin = false;
        gameover = false;
        instance = this;
        canAudio = PlayerPrefs.GetInt("Audio", 1) == 1;
        if (canAudio) {
            audioOnOff.SetActive(false);
            Camera.main.GetComponent<AudioSource>().Play();
        }
        else {
            audioOnOff.SetActive(true);
            Camera.main.GetComponent<AudioSource>().Stop();
        }
        beginPanel.SetActive(true);
    }
    void Start() {
        models = new ModelBase[xCol, yRow];
        //为字典赋值
        modelPrefabDict = new Dictionary<ModelType, GameObject>();
        foreach (var mp in modelPrefabs) {
            if (!modelPrefabDict.ContainsKey(mp.type)) {
                modelPrefabDict.Add(mp.type, mp.prefab);
            }
        }
        //实例化格子
        for (int x = 0; x < xCol; x++) {
            for (int y = 0; y < yRow; y++) {
                //生成格子
                GameObject grid = Instantiate(gridPrefab, CalGridPos(x, y), Quaternion.identity);
                grid.transform.parent = this.transform;//将格子的父物体设置为GameManager
            }
        }
        //实例化模型
        for (int x = 0; x < xCol; x++) {
            for (int y = 0; y < yRow; y++) {
                CreatNewModel(x, y, ModelType.Empty);
            }
        }

    }

    void Update() {
        //Debug.Log(step);
        if (gameover) {
            return;
        }
        time += Time.deltaTime;
        time %= 10;
        //Debug.Log(time);
        if (gameTime <= 0) {
            gameTime = 0;
            //TODO:失败处理
            gameover = true;
            gameoverPanel.SetActive(true);
            GameOver();
            return;
        }
        if (gameBegin) {
            gameTime -= Time.deltaTime;
            restTimeText.text = gameTime.ToString("0");//0取整,0.0保留一位小数,0.00保留两位小数......
            scoreText.text = score + "";
        }
        else {
            score = 0;
        }
    }
    //计算格子的位置坐标
    public Vector3 CalGridPos(int x, int y) {
        return new Vector3(transform.position.x - xCol / 2f * 0.56f + x * 0.65f, transform.position.y + yRow / 2f * 0.15f - y * 0.65f);
    }
    //产生model的方法
    public ModelBase CreatNewModel(int x, int y, ModelType type) {
        GameObject newModel = Instantiate(modelPrefabDict[type], CalGridPos(x, y), Quaternion.identity);
        newModel.transform.parent = transform;
        models[x, y] = newModel.GetComponent<ModelBase>();
        models[x, y].Init(x, y, this, type);
        return models[x, y];
    }
    //全部填充
    public IEnumerator FillAll(float t) {
        bool needFill = true;
        while (needFill) {
            yield return new WaitForSeconds(t);
            while (Fill(t)) {
                yield return new WaitForSeconds(t);
            }
            //清除匹配的model
            needFill = ClearAllMatchModels();
        }
    }
    //分布填充
    public bool Fill(float t) {
        bool notFinished = false;//本次填充是否完成
        for (int y = yRow - 2; y >= 0; y--) {
            for (int x = 0; x < xCol; x++) {
                ModelBase model = models[x, y];//当前元素的基础组件
                //向下填充空缺
                if (model.CanMove()) {
                    ModelBase modelBelow = models[x, y + 1];//正下方model组件
                    if (modelBelow.Type == ModelType.Empty) {//垂直填充
                        if (modelBelow.gameObject != null) {
                            Destroy(modelBelow.gameObject);
                            model.ModelMoveComponent.Move(x, y + 1, t);//向下移动
                            models[x, y + 1] = model;//正下方的组件指向当前组件
                            if (Random.Range(0,100)==1) {
                                CreatNewModel(x, y, ModelType.Wall);//设置障碍物
                            }
                            else {
                                CreatNewModel(x, y, ModelType.Empty);//当前元素置空
                            }
                            notFinished = true;
                        }
                        else {
                            CreatNewModel(x, y + 1, ModelType.Empty);//生成一个空物体
                        }
                    }
                    //斜向填充,用于解决存在障碍物的情况
                    else {
                        for (int down = -8; down <= 8; down++) {
                            if (down != 0) {
                                int downX = x + down;
                                if (downX >= 0 && downX < xCol) {//排除最右侧
                                    ModelBase downModel = models[Mathf.Clamp(downX,0,7), y + 1];
                                    if (downModel.Type == ModelType.Empty) {
                                        bool canFill = true;//是否满足垂直填充
                                        for (int aboveY = y; aboveY >= 0; aboveY--) {
                                            ModelBase modelAbove = models[downX, aboveY];
                                            if (modelAbove.CanMove()) {
                                                break;
                                            }
                                            else if (!modelAbove.CanMove() && modelAbove.Type != ModelType.Empty) {
                                                canFill = false;
                                                break;
                                            }
                                        }
                                        //斜向填充
                                        if (!canFill) {
                                            if (downModel.gameObject != null) {
                                                Destroy(downModel.gameObject);
                                                model.ModelMoveComponent.Move(downX, y + 1, t);
                                                models[downX, y + 1] = model;
                                                CreatNewModel(x, y, ModelType.Empty);
                                                notFinished = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        //最底下的一层
        for (int x = 0; x < xCol; x++) {
            ModelBase model = models[x, 0];//当前元素的基础组件
            if (model.Type == ModelType.Empty) {
                //在y坐标为-1的位置生成
                GameObject newModel = Instantiate(modelPrefabDict[ModelType.Normal], CalGridPos(x, -1), Quaternion.identity);
                newModel.transform.parent = this.transform;//设置父物体
                models[x, 0] = newModel.GetComponent<ModelBase>();//更新基础组件位置
                models[x, 0].Init(x, -1, this, ModelType.Normal);//初始化
                if (models[x, 0].CanMove()) {
                    models[x, 0].ModelMoveComponent.Move(x, 0, fillTime);//向下移动
                }
                //随机一个颜色
                models[x, 0].ModelColorComponent.SetColor((ModelColor.ColorType)Random.Range(0, models[x, 0].ModelColorComponent.Nums));
                notFinished = true;
            }
        }
        return notFinished;
    }
    //是否相邻判定
    public bool IsNeighbor(ModelBase m1, ModelBase m2) {
        return m1.X == m2.X && Mathf.Abs(m1.Y - m2.Y) == 1 || m1.Y == m2.Y && Mathf.Abs(m1.X - m2.X) == 1;
    }
    //交换model
    private void ExchangeModel(ModelBase m1, ModelBase m2) {
        //Debug.Log(selectModel.ModelColorComponent.Color+" "+targetModel.ModelColorComponent.Color);
        if (m1.CanMove() && m2.CanMove()) {
            models[m1.X, m1.Y] = m2;
            models[m2.X, m2.Y] = m1;
            if (step==0) {
                if (MatchModels(m2, m1.X, m1.Y) != null || MatchModels(m1, m2.X, m2.Y) != null || m1.Type == ModelType.RainBow || m2.Type == ModelType.RainBow) {
                    int tempX = m1.X;
                    int tempY = m1.Y;
                    m1.ModelMoveComponent.Move(m2.X, m2.Y, fillTime);//交换
                    m2.ModelMoveComponent.Move(tempX, tempY, fillTime);
                    if (m1.Type == ModelType.RainBow && m1.CanClear() && m2.CanClear()) {
                        ModelColorClearByType mcct = m1.transform.GetComponent<ModelColorClearByType>();
                        //Debug.Log(mcct == null);
                        if (mcct != null) {
                            mcct.Color = m2.ModelColorComponent.Color;
                            ClearByType(mcct.Color);
                        }
                        m1.ModelClearComponent.Clear();
                        models[m1.X, m1.Y] = CreatNewModel(m1.X, m1.Y, ModelType.Empty);
                        ClearModel(m1.X, m1.Y);
                        //StartCoroutine(FillAll(fillTime));//将消除后的空位进行填充
                    }
                    if (m2.Type == ModelType.RainBow && m2.CanClear() && m1.CanClear()) {
                        ModelColorClearByType mcct = m2.transform.GetComponent<ModelColorClearByType>();
                        //Debug.Log(mcct == null);
                        if (mcct != null) {
                            mcct.Color = m1.ModelColorComponent.Color;
                            ClearByType(mcct.Color);
                        }
                        m2.ModelClearComponent.Clear();
                        models[m2.X, m2.Y] = CreatNewModel(m2.X, m2.Y, ModelType.Empty);
                        ClearModel(m2.X, m2.Y);
                        //StartCoroutine(FillAll(time));//将消除后的空位进行填充
                    }
                    ClearAllMatchModels();//清除所有匹配的model
                    StartCoroutine(FillAll(fillTime));//将消除后的空位进行填充
                    selectModel = null;
                    targetModel = null;
                    step = 0;
                    curStep.text = "0";
                }
                else {
                    int tempX = m1.X;
                    int tempY = m1.Y;
                    m1.ModelMoveComponent.Move(m2.X, m2.Y, fillTime);//交换
                    m2.ModelMoveComponent.Move(tempX, tempY, fillTime);
                    step++;
                    targetModel = null;
                    curStep.text = "1";
                }
            }
            else {
                if (MatchModels(m2, m1.X, m1.Y) != null || MatchModels(m1, m2.X, m2.Y) != null || m1.Type == ModelType.RainBow || m2.Type == ModelType.RainBow) {
                    int tempX = m1.X;
                    int tempY = m1.Y;
                    m1.ModelMoveComponent.Move(m2.X, m2.Y, fillTime);//交换
                    m2.ModelMoveComponent.Move(tempX, tempY, fillTime);
                    if (m1.Type == ModelType.RainBow && m1.CanClear() && m2.CanClear()) {
                        ModelColorClearByType mcct = m1.transform.GetComponent<ModelColorClearByType>();
                        //Debug.Log(mcct == null);
                        if (mcct != null) {
                            mcct.Color = m2.ModelColorComponent.Color;
                            ClearByType(mcct.Color);
                        }
                        m1.ModelClearComponent.Clear();
                        models[m1.X, m1.Y] = CreatNewModel(m1.X, m1.Y, ModelType.Empty);
                        ClearModel(m1.X, m1.Y);
                        //StartCoroutine(FillAll(fillTime));//将消除后的空位进行填充
                    }
                    if (m2.Type == ModelType.RainBow && m2.CanClear() && m1.CanClear()) {
                        ModelColorClearByType mcct = m2.transform.GetComponent<ModelColorClearByType>();
                        //Debug.Log(mcct == null);
                        if (mcct != null) {
                            mcct.Color = m1.ModelColorComponent.Color;
                            ClearByType(mcct.Color);
                        }
                        m2.ModelClearComponent.Clear();
                        models[m2.X, m2.Y] = CreatNewModel(m2.X, m2.Y, ModelType.Empty);
                        ClearModel(m2.X, m2.Y);
                        //StartCoroutine(FillAll(time));//将消除后的空位进行填充
                    }
                    ClearAllMatchModels();//清除所有匹配的model
                    StartCoroutine(FillAll(fillTime));//将消除后的空位进行填充
                    selectModel = null;
                    targetModel = null;
                    step = 0;
                    curStep.text = "0";
                }
                else {
                    //还原基础脚本
                    models[m1.X, m1.Y] = m1;
                    models[m2.X, m2.Y] = m2;
                    models[m1.X, m1.Y].ModelMoveComponent.Undo(m1, m2, fillTime);//交换位置再还原
                    step = 1;
                    curStep.text = "1";
                }
            }
        }
        //StartCoroutine(FillAll(fillTime));//将消除后的空位进行填充
    }
    //选中对象
    public void SelectModel(ModelBase m) {
        if (gameover) {
            return;
        }
        lastSelectModel = selectModel;
        selectModel = m;
    }
    //目标对象
    public void TargetModel(ModelBase m) {
        if (gameover) {
            return;
        }
        targetModel = m;
    }
    //鼠标抬起,model交换
    public void ReleaseModel() {
        if (gameover) {
            return;
        }
        if (IsNeighbor(selectModel, targetModel)) {
            ExchangeModel(selectModel, targetModel);
        }
        StartCoroutine(FillAll(fillTime));//将消除后的空位进行填充
    }
    //匹配model
    public List<ModelBase> MatchModels(ModelBase model, int newX, int newY) {
        if (model.CanColor()) {
            ModelColor.ColorType color = model.ModelColorComponent.Color;
            List<ModelBase> matchRow = new List<ModelBase>();//存取行
            List<ModelBase> matchCol = new List<ModelBase>();//存取列
            List<ModelBase> match = new List<ModelBase>();//存取全部可消除的列表
            //行匹配
            matchRow.Add(model);
            //i=0代表往左，i=1代表往右
            for (int i = 0; i <= 1; i++) {
                for (int xDistance = 1; xDistance < xCol; xDistance++) {
                    int x;
                    if (i == 0) {
                        x = newX - xDistance;
                    }
                    else {
                        x = newX + xDistance;
                    }
                    if (x < 0 || x >= xCol) {
                        break;
                    }
                    if (models[x, newY].CanColor() && models[x, newY].ModelColorComponent.Color == color) {
                        matchRow.Add(models[x, newY]);
                    }
                    else {
                        break;
                    }
                }
            }
            if (matchRow.Count >= 3) {
                foreach (var r in matchRow) {
                    match.Add(r);
                }
            }
            //L T型匹配
            //检查一下当前行遍历列表中的元素数量是否大于3
            if (matchRow.Count >= 3) {
                for (int i = 0; i < matchRow.Count; i++) {
                    //行匹配列表中满足匹配条件的每个元素上下依次进行列遍历
                    // 0代表上方 1代表下方
                    for (int j = 0; j <= 1; j++) {
                        for (int yDistance = 1; yDistance < yRow; yDistance++) {
                            int y;
                            if (j == 0) {
                                y = newY - yDistance;
                            }
                            else {
                                y = newY + yDistance;
                            }
                            if (y < 0 || y >= yRow) {
                                break;
                            }
                            if (models[matchRow[i].X, y].CanColor() && models[matchRow[i].X, y].ModelColorComponent.Color == color) {
                                matchCol.Add(models[matchRow[i].X, y]);
                            }
                            else {
                                break;
                            }
                        }
                    }
                    if (matchCol.Count < 2) {
                        matchCol.Clear();
                    }
                    else {
                        for (int j = 0; j < matchCol.Count; j++) {
                            match.Add(matchCol[j]);
                        }
                        break;
                    }
                }
            }
            //if (match.Count >= 3) {
            //    return match;
            //}
            matchRow.Clear();
            matchCol.Clear();
            matchCol.Add(model);
            //列匹配
            //i=0代表往左，i=1代表往右
            for (int i = 0; i <= 1; i++) {
                for (int yDistance = 1; yDistance < yRow; yDistance++) {
                    int y;
                    if (i == 0) {
                        y = newY - yDistance;
                    }
                    else {
                        y = newY + yDistance;
                    }
                    if (y < 0 || y >= yRow) {
                        break;
                    }
                    if (models[newX, y].CanColor() && models[newX, y].ModelColorComponent.Color == color) {
                        matchCol.Add(models[newX, y]);
                    }
                    else {
                        break;
                    }
                }
            }
            if (matchCol.Count >= 3) {
                for (int i = 0; i < matchCol.Count; i++) {
                    match.Add(matchCol[i]);
                }
            }
            //L T型匹配
            //检查一下当前行遍历列表中的元素数量是否大于3
            if (matchCol.Count >= 3) {
                for (int i = 0; i < matchCol.Count; i++) {
                    //行匹配列表中满足匹配条件的每个元素上下依次进行列遍历
                    // 0代表上方 1代表下方
                    for (int j = 0; j <= 1; j++) {
                        for (int xDistance = 1; xDistance < xCol; xDistance++) {
                            int x;
                            if (j == 0) {
                                x = newX - xDistance;
                            }
                            else {
                                x = newX + xDistance;
                            }
                            if (x < 0 || x >= xCol) {
                                break;
                            }
                            if (models[x, matchCol[i].Y].CanColor() && models[x, matchCol[i].Y].ModelColorComponent.Color == color) {
                                matchRow.Add(models[x, matchCol[i].Y]);
                            }
                            else {
                                break;
                            }
                        }
                    }
                    if (matchRow.Count < 2) {
                        matchRow.Clear();
                    }
                    else {
                        for (int j = 0; j < matchRow.Count; j++) {
                            match.Add(matchRow[j]);
                        }
                        break;
                    }
                }
            }
            if (match.Count >= 3) {
                return match;
            }
        }
        return null;
    }
    //清除模块
    #region Clear Module
    //清除model
    public bool ClearModel(int x, int y) {
        //当前model可以清除并且没有正在清除
        if (models[x, y].CanClear() && models[x, y].ModelClearComponent.IsClearing == false) {
            if (models[x, y].Type != ModelType.CrossClear && models[x, y].Type != ModelType.RainBow) {
                models[x, y].ModelClearComponent.Clear();//将model清除掉
                CreatNewModel(x, y, ModelType.Empty);//原地生成一个新的空类型
                ClearRoadblock(x, y);//清除障碍物
                if (gameBegin) {
                    PlayerPrefs.SetInt("ClearModelNums", PlayerPrefs.GetInt("ClearModelNums", 0) + 1);//记录消除块数
                }
                return true;
            }
        }
        return false;
    }
    //清除障碍物
    public void ClearRoadblock(int x, int y) {//被消除model的坐标
        for (int nearX = x - 1; nearX <= x + 1; nearX++) {
            //若不为自身,未超出格子边界,类型为wall,可以清除
            if (nearX != x && nearX >= 0 && nearX < xCol) {
                if (models[nearX, y].CanClear() && models[nearX, y].Type == ModelType.Wall) {
                    //Debug.Log("clear");
                    models[nearX, y].HP --;
                    if (models[nearX, y].HP==0) {
                        models[nearX, y].ModelClearComponent.Clear();//消除障碍物
                        CreatNewModel(nearX, y, ModelType.Empty);//原地置空等待填充
                    }
                }
            }
        }
        for (int nearY = y - 1; nearY <= y + 1; nearY++) {
            //若不为自身,未超出格子边界,类型为wall,可以清除
            if (nearY != y && nearY >= 0 && nearY < yRow) {
                if (models[x, nearY].CanClear() && models[x, nearY].Type == ModelType.Wall) {
                    //Debug.Log("clear");
                    models[x, nearY].ModelClearComponent.Clear();//消除障碍物
                    if (gameBegin) {
                        PlayerPrefs.SetInt("WallNums", PlayerPrefs.GetInt("WallNums", 0) + 1);//记录消除墙体个数
                    }
                    CreatNewModel(x, nearY, ModelType.Empty);//原地置空等待填充
                }

            }
        }
    }
    //清除匹配的model列表
    public bool ClearAllMatchModels() {
        bool needFill = false;
        for (int y = 0; y < yRow; y++) {
            for (int x = 0; x < xCol; x++) {
                if (models[x, y].CanClear()) {
                    List<ModelBase> matchList = MatchModels(models[x, y], x, y);
                    if (matchList != null) {
                        int num = matchList.Count;
                        //根据消除个数处理分数
                        if (gameBegin) {
                            switch (num) {
                                case 3:
                                    scoreStep = 10;
                                    score += scoreStep;
                                    break;
                                case 4:
                                    Excellent(0);
                                    scoreStep = 20;
                                    score += scoreStep;
                                    break;
                                case 5:
                                    scoreStep = 30;
                                    score += scoreStep;
                                    Excellent(1);
                                    gameTime += 3;
                                    PlayerPrefs.SetInt("TimeAddNums", PlayerPrefs.GetInt("TimeAddNums", 0) + 3);//记录累计加时
                                    break;
                                case 6:
                                    scoreStep = 60;
                                    score += scoreStep;
                                    Excellent(2);
                                    gameTime += 5;
                                    PlayerPrefs.SetInt("TimeAddNums", PlayerPrefs.GetInt("TimeAddNums", 0) + 5);//记录累计加时
                                    break;
                                case 7:
                                    scoreStep = 100;
                                    score += scoreStep;
                                    Excellent(3);
                                    gameTime += 10;
                                    PlayerPrefs.SetInt("TimeAddNums", PlayerPrefs.GetInt("TimeAddNums", 0) + 10);//记录累计加时
                                    break;
                            }
                        }
                       
                        //生成奖励块
                        ModelType specialModelType = ModelType.Count;//是否产生特殊奖励
                        ModelBase model = matchList[0];
                        int specialModelX = model.X;
                        int specialModelY = model.Y;
                        if (num >= 3) {
                            if ( Random.Range(0, 3) == 0) {
                                specialModelType = ModelType.CrossClear;
                            }
                            else if (Random.Range(0, 3) == 1) {
                                //Debug.Log(matchList.Count);
                                specialModelType = ModelType.RainBow;
                            }
                        }
                        foreach (var m in matchList) {
                            if (ClearModel(m.X, m.Y)) {
                                needFill = true;
                            }
                        }
                        if (specialModelType != ModelType.Count && (time >= 2f && time < 2.1f) || (time >= 4f && time < 4.1f) || (time >= 6f && time < 6.1f) || (time >= 8f && time < 8.1f)) {
                          //if (specialModelType != ModelType.Count) {
                            Destroy(models[specialModelX, specialModelY]);
                            specialModelType = Random.Range(0, 2) == 1 ? ModelType.CrossClear : ModelType.RainBow;
                            ModelBase newModel = CreatNewModel(specialModelX, specialModelY, specialModelType);
                            if (Random.Range(0, 3) == 2) {
                                //十字消除
                                if (specialModelType == ModelType.CrossClear && newModel.CanColor() && matchList[0].CanColor()) {
                                    newModel.ModelColorComponent.SetColor(ModelColor.ColorType.Cross);
                                }
                                //类型消除的产生
                                else if (specialModelType == ModelType.RainBow && newModel.CanColor()) {
                                    newModel.ModelColorComponent.SetColor(ModelColor.ColorType.Rainbow);
                                }
                            }
                            models[specialModelX, specialModelY] = newModel;
                        }
                    }
                }
            }
        }
        return needFill;
    }
    //同类型消除
    public void ClearByType(ModelColor.ColorType color) {
        if (gameBegin) {
            PlayerPrefs.SetInt("RainbowNums", PlayerPrefs.GetInt("RainbowNums", 0) + 1);//记录彩虹个数
            awardNums++;
            PlayerPrefs.SetInt("LuckDog", awardNums);
            //Debug.Log("qingchucaihong");
            int count = 0;
            for (int x = 0; x < xCol; x++) {
                for (int y = 0; y < yRow; y++) {
                    if (models[x, y].CanColor() && (models[x, y].ModelColorComponent.Color == color || color == ModelColor.ColorType.Rainbow)) {
                        count++;
                        //Debug.Log(models.);
                        ClearModel(x, y);
                    }
                }
            }
            score += 5 * count;
            StartCoroutine(FillAll(fillTime));
        }
    }
    //十字消除
    public void ClearCross(int x, int y) {
        if (gameBegin) {
            PlayerPrefs.SetInt("CrossNums", PlayerPrefs.GetInt("CrossNums", 0) + 1);//记录十字个数
            awardNums++;
            //Debug.Log("cross");
            for (int i = 0; i < xCol; i++) {
                ClearModel(i, y);
            }
            for (int j = 0; j < yRow; j++) {
                if (j != y) {
                    ClearModel(x, j);
                }
            }
            score += 80;
            if (models[x,y]!=null) {
                models[x, y].ModelClearComponent.Clear();
                models[x, y] = CreatNewModel(x, y, ModelType.Empty);
            }
            StartCoroutine(FillAll(fillTime));
        }
    }
    #endregion
    //处理UI界面的事件
    #region UI Events
    public void Excellent(int index) {
        Instantiate(excellent[index], spawn);
        if (index!=0) {
            GameObject go = Instantiate(timeAddEffetc[index], canvas);
            switch (index) {
                case 1: go.GetComponent<RectTransform>().anchoredPosition = new Vector2(700, 800);break;
                case 2: go.GetComponent<RectTransform>().anchoredPosition = new Vector2(774, 697); break;
                case 3: go.GetComponent<RectTransform>().anchoredPosition = new Vector2(774, 697); break;
            }
            go.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 180));
        }
    }
    public void Pause() {
        //pausePanel.GetComponent<Animator>().SetTrigger("close");
        pausePanel.SetActive(true);
        pausePanel.GetComponent<Animator>().SetTrigger("open");
        if (canAudio) {
            AudioSource.PlayClipAtPoint(audios[0], Camera.main.transform.position, 1);
        }
    }

    public void Resume() {
        Time.timeScale = 1;
        pausePanel.GetComponent<Animator>().SetTrigger("close");
        if (canAudio) {
            AudioSource.PlayClipAtPoint(audios[1], Camera.main.transform.position, 1);
        }
    }

    public void Replay() {
        Time.timeScale = 1;
        if (gameoverPanel.activeInHierarchy) {
            gameoverPanel.GetComponent<Animator>().SetTrigger("close");
        }
        SceneManager.LoadScene(1);
    }
    public void Quit() {
        Time.timeScale = 1;
        gameoverPanel.GetComponent<Animator>().SetTrigger("close");
        SceneManager.LoadScene(0);
    }
    //界面显示
    public void AboutUsDisplay() {
        Time.timeScale = 1;
        pausePanel.GetComponent<Animator>().SetTrigger("close");
        aboutUs.SetActive(true);
        aboutUs.GetComponent<Animator>().SetTrigger("display");
        if (canAudio) {
            AudioSource.PlayClipAtPoint(audios[0], Camera.main.transform.position, 1);
        }
    }
    public void AboutUsClose() {
        Time.timeScale = 1;
        aboutUs.GetComponent<Animator>().SetTrigger("close");
        if (canAudio) {
            AudioSource.PlayClipAtPoint(audios[1], Camera.main.transform.position, 1);
        }
    }
    public void AudioController() {
        if (canAudio) {
            canAudio = false;
            audioOnOff.SetActive(true);
            Camera.main.GetComponent<AudioSource>().Stop();
            PlayerPrefs.SetInt("Audio", 0);
        }
        else {
            canAudio = true;
            audioOnOff.SetActive(false);
            Camera.main.GetComponent<AudioSource>().Play();
            PlayerPrefs.SetInt("Audio", 1);
        }
    }
    //游戏结束的逻辑
    public void GameOver() {
        if (awardNums>PlayerPrefs.GetInt("LuckDog",0)) {
            PlayerPrefs.SetInt("LuckDog",awardNums);
        }
        PlayerPrefs.SetInt("TotalScore", (PlayerPrefs.GetInt("TotalScore", 0) + score));//记录累计分数
        Debug.Log(score+" "+ PlayerPrefs.GetInt("TotalScore", 0));
        if (score > PlayerPrefs.GetInt("HistoryHighestScore", 0)) {
            PlayerPrefs.SetInt("HistoryHighestScore", score);
            breakRecord.SetActive(true);
        }
        gameoverPanel.GetComponent<Animator>().SetTrigger("display");
        overScoreText.text = score.ToString();
        historySoreText.text = PlayerPrefs.GetInt("HistoryHighestScore").ToString();
        StartCoroutine(FillAll(0.1f));
    }
    #endregion

}
