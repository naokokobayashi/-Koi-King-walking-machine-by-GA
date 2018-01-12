//-------------------------------------------------------
//　知的情報処理の課題提出用GAアルゴリズム
//　コイキングがより高く飛び跳ねるプログラムを目指す
//　参考コード：http://developer.wonderpla.net/entry/blog/engineer/GeneticAlgorithm/
//-------------------------------------------------------


﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GenManager : MonoBehaviour {

	//-----------------------------------------------------
	//【定数定義】
	//-----------------------------------------------------
	public GenParam param;

	public GameObject creature;
	public int creatureCount   = 100;　　//　個体の総数
	public int surviveCount  = 10;　 　  //　親世代から子世代へ受け継がれる個体の数　
	public int mutationCount = 10;　　 　//　突然変異する個体の数

	protected Creature[] currentCreatures;

	public float genDuration = 20;      //　1世代あたりの時間<s>
	protected float genDurationLeft;    //　現在の世代の残り時間

	public string namePrefix;
	public string importPath;

	public int genCount;　　　　　 　　　//　世代数

	public Vector3 finRotateLimit = new Vector3(30,30,30);　　　//　ひれの回転の制限（度）
  public Vector3 bodyRotateLimit = new Vector3(20,20,20);　　　//　体の回転の制限（度）
  //public float[] max_position_y;
  public float[] max_position_z;

	protected float[] bestScores = new float[10];
	protected int[] bestScoreIds = new int[10];


	//-----------------------------------------------------
	//【関数定義】初期化（オブジェクト起動時に実行される）
	//-----------------------------------------------------
	void Start () {

		if( importPath.Length > 0 ) {
			// 遺伝データを読み込む
			System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer( typeof(GenParam) );
			System.IO.FileStream fs = new System.IO.FileStream(importPath, System.IO.FileMode.Open);
			GenParam p = (GenParam)xs.Deserialize(fs);

			this.param = p;
			this.genCount = p.genCount;
		}
		else {
			param.creatureCount = this.creatureCount;
			param.surviveCount = this.surviveCount;
			param.mutationCount = this.mutationCount;

			initParam();
		}

		//　新しく個体をcreaturecount個生成
		currentCreatures = new Creature[param.creatureCount];

		prepareCreatures();
		genDurationLeft = genDuration;

	  //　カメラの位置は固定（右が進行方向）
 　 Camera.main.transform.position = new Vector3(100, 10, 0);
    Camera.main.transform.LookAt(new Vector3(-100, 10, 0));

	}

	//-----------------------------------------------------
	//【関数定義】パラメータの初期化
	//-----------------------------------------------------
	void initParam() {

		param.genCount = 1;
		param.creatureParams = new CreatureParam[param.creatureCount];
    max_position_z = new float[param.creatureCount];
    //max_position_y = new float[param.creatureCount];

　　// 生物の各個体にパラメータを設定
		for( int i=0; i<param.creatureCount; ++i ) {
		  max_position_z[i] = 0;
			//max_position_y[i] = 0;
			param.creatureParams[i].finParams = new finParam[6];

　　　 //　関節は6個
			for( int j=0; j<6; ++j ) {

				finParam lp = param.creatureParams[i].finParams[j];　　
				if( j<3 ){
				    lp.RotRange = new Vector3(
					    	Random.Range(-bodyRotateLimit.x, bodyRotateLimit.x),
						    Random.Range(-bodyRotateLimit.y, bodyRotateLimit.y),
						    Random.Range(-bodyRotateLimit.z, bodyRotateLimit.z));
					}else{
						lp.RotRange = new Vector3(
								Random.Range(-finRotateLimit.x, finRotateLimit.x),
								Random.Range(-finRotateLimit.y, finRotateLimit.y),
								Random.Range(-finRotateLimit.z, finRotateLimit.z));
					}

				param.creatureParams[i].finParams[j] = lp;
			}
		}

	}


	//-----------------------------------------------------
	//【関数定義】次の世代の準備（毎世代の初めに呼ばれる）
	//-----------------------------------------------------
	void prepareCreatures() {
		for( int i=0; i<param.creatureCount; ++i ) {
			// プレハブを取得
      GameObject creature = (GameObject)Resources.Load ("Prefabs/Creature");
			//　プレハブの生成
			//　プレハブ名、position,Quaternion.identity
			GameObject obj = Instantiate(creature, new Vector3((i - param.creatureCount/2) * 15, 5.0f, 0),Quaternion.identity);
			Creature ws = obj.GetComponent<Creature>();
			ws.Param = param.creatureParams[i];
			currentCreatures[i] = ws;
		  max_position_z[i] = 0;
		  //max_position_y[i] = 0;
		}
	}


	//-----------------------------------------------------
	//【関数定義】個体の削除
	//-----------------------------------------------------
	void deleteCreatures() {
		for( int i=0; i<param.creatureCount; ++i ) {
			Destroy(currentCreatures[i].gameObject);
			currentCreatures[i] = null;
		}
	}


	//-----------------------------------------------------
	//【関数定義】更新（残り時間の計算、外部ファイルへの記録、カメラの移動）
	//-----------------------------------------------------
	// Update is called once per frame
	void FixedUpdate () {

		genDurationLeft -= Time.fixedDeltaTime;

		//　各個体の高さの最高値を記録
	  for( int i=0; i<param.creatureCount; ++i ) {
			Creature creature = currentCreatures[i];
			if( max_position_z[i] < creature.transform.position.z )
   		  max_position_z[i] = creature.transform.position.z;
		}

    /*
		//　各個体の高さの最高値を記録
	  for( int i=0; i<param.creatureCount; ++i ) {
			Creature creature = currentCreatures[i];
			if( max_position_y[i] < creature.transform.position.y )
   		  max_position_y[i] = creature.transform.position.y;
		}
		*/

		//　残り時間が0になった場合の処理
		if( genDurationLeft < 0 ) {
			calcNextGenParams();
			deleteCreatures();
			prepareCreatures();
			genDurationLeft = genDuration;
			param.genCount++;

			// 20世代毎にデータを保存
			if( param.genCount % 20 == 0 ) {
				string dateStr = System.DateTime.Now.ToString("yyyyMMddhhmm");
				string path = string.Format("Export/Gen_{0}_{1}.xml", param.genCount, dateStr);

				System.Xml.Serialization.XmlSerializer xs = new System.Xml.Serialization.XmlSerializer( typeof(GenParam) );
				System.IO.FileStream fs = new System.IO.FileStream(path, System.IO.FileMode.Create);
				xs.Serialize(fs, param);
				fs.Close();
			}
		}
	}

	//-----------------------------------------------------
	//【関数定義】 文字表示（世代数など）
	//-----------------------------------------------------
	void OnGUI() {
		string str = "";
		str += string.Format("Generation: {0}\n", param.genCount);
		str += string.Format("Time: {0}\n", genDurationLeft);
		str += string.Format("\n", genDurationLeft);
		str += string.Format("Best score\n");
		for(int i=0; i<10; ++i ) {
			str += string.Format("  {0:D2}: {1:F2}\n", bestScoreIds[i], bestScores[i]);
		}

		GUIStyle style = new GUIStyle();
		style.normal.textColor = Color.black;
		GUI.Label(new Rect(10, 10, 100, 40), str, style);
	}


　//-----------------------------------------------------
	//【関数定義】選択
	//　引数　scoreList：辞書型のリスト。個体番号とそれぞれのスコア（飛距離）を記録
	//　戻り値　surviver：次の個体にパラメータがそのまま受け継がれる個体のデータの集合
	//　その他　CreatureParam：個体（コイキング）のデータ。各関節における、軸ごとの回転値を保持
	//　コメント　現在は飛距離のみで評価。選択方法のアイディアだけでもください！！（減点とか）
　//-----------------------------------------------------
  protected CreatureParam[] Select(Dictionary<int, float> scoreList){

		CreatureParam[] surviver = new CreatureParam[param.surviveCount];
		int sc = 0;
		// OrderByDescending : 降順にソート(valueをソート時のキーとする)
		foreach(KeyValuePair<int, float> pair in scoreList.OrderByDescending(p => p.Value) ) {
			surviver[sc]= param.creatureParams[pair.Key];

			//　より前に進んだ個体を10個記録する
			if( sc < 10 ) {
				bestScores[sc] = pair.Value;　//　進んだ距離
				bestScoreIds[sc] = pair.Key;　//　個体番号
			}
			sc++;
			if( sc >= param.surviveCount ) break;　//　次の世代に残す個体数がsc(<=10)を下回ったら終了
		}

		return surviver;
	}


	//-----------------------------------------------------
	//【関数定義】交叉
	//　引数　surviver：次の個体にパラメータがそのまま受け継がれる個体のデータの集合
	//　戻り値　np：交叉後の個体（コイキング）のデータ
	//　その他　surviveCount ：今世代の優秀な個体（親となる）の個体数。現在の設定は10
	//　　　　　CreatureParam：個体（コイキング）のデータ。各関節における、軸ごとの回転値を保持
	//　　　　　finParams    ：CreatureParamが保持している回転値（↑）
	//　コメント　正直これ以上変えようがない気がする
	//-----------------------------------------------------
  protected CreatureParam Cross(CreatureParam[] surviver){

			List<int> indices = new List<int>();
			for( int j=0; j<param.surviveCount; ++j ) {
				indices.Add(j);
			}

		　//　今世代の優秀な個体のうち、2体をランダムに選ぶ　＝　親
			int i1 = indices[Random.Range(0,param.surviveCount)];
			indices.Remove(i1);
			int i2 = indices[Random.Range(0,param.surviveCount-1)];

			CreatureParam[] cp = new CreatureParam[]{ surviver[i1], surviver[i2] };

			// copy fin params ramdomely
			CreatureParam np;

			np.finParams = new finParam[6];
			//　6つの関節に関してそれぞれ、上記で選んだ親のどちらかのパラメータをコピーする
			for( int j=0; j<6; ++j ) {
				np.finParams[j] = cp[ Random.Range(0,2) ].finParams[j];
			}

		  return np;
	}


  //-----------------------------------------------------
	//【関数定義】突然変異
	//　引数　np：突然変異する個体のデータ
	//　戻り値　np：突然変異後の個体のデータ
	//　その他　finRotateLimit：各関節の回転の制限値で、x,y,z軸方向に今はそれぞれ60度（変更予定）
	//　　　　　RotRange      ：各関節の回転範囲。finRotateLimit（↑）の範囲内でランダムで決定している
	//　　　　　CreatureParam ：個体（コイキング）のデータ。各関節における、軸ごとの回転値を保持
	//　　　　　finParams     ：CreatureParamが保持している回転値（↑）
	//　コメント　変えるとしたら呼び出し元の関数かも（無能）
	//-----------------------------------------------------
　protected CreatureParam Mutation(CreatureParam np){
	  //　関節のパラメータを1~4回ランダムで変更する
	  //　（同じ関節が複数回変更されることもある）
	  int mpc = Random.Range(1,4);
	  for( int j=0; j<mpc; ++j ) {

　　//　パラメータを突然変異させる関節をランダムに選ぶ
		int lr = Random.Range(0,6);
		finParam lp = np.finParams[lr];
		if( lr<3 ){
				lp.RotRange = new Vector3(
						Random.Range(-bodyRotateLimit.x, bodyRotateLimit.x),
						Random.Range(-bodyRotateLimit.y, bodyRotateLimit.y),
						Random.Range(-bodyRotateLimit.z, bodyRotateLimit.z));
			}else{
				lp.RotRange = new Vector3(
						Random.Range(-finRotateLimit.x, finRotateLimit.x),
						Random.Range(-finRotateLimit.y, finRotateLimit.y),
						Random.Range(-finRotateLimit.z, finRotateLimit.z));
			}

    np.finParams[lr] = lp;
	  }

	  return np;
  }


	//-----------------------------------------------------
	//【関数定義】次の世代の個体の設定
	//-----------------------------------------------------
	protected void calcNextGenParams() {

　　//　辞書型のリストを定義
　　//　creatureCount<int>, score<float>を格納する
		Dictionary<int, float> scoreList = new Dictionary<int, float>();

		for( int i=0; i<param.creatureCount; ++i ) {
			//Creature creature = currentCreatures[i];
			// スコア（飛んだ高さ）を計算
			float score = max_position_z[i] ;
			//float score = max_position_y[i] ;
			//float score = creature.transform.position.y;
			scoreList.Add(i, score);
		}


		//-----------------------------------------------------
	　//　選択
		//-----------------------------------------------------
    CreatureParam[] surviver = Select(scoreList);

	  //　今世代の優秀な個体を、次の世代にコピーする
		for( int i=0; i<param.surviveCount; ++i ) {
			param.creatureParams[i] = surviver[i];
		}

		//　次の世代に新たに生成する個体の数を計算
		int newCount = param.creatureCount - param.surviveCount;


		//-----------------------------------------------------
	　//　交叉
		//-----------------------------------------------------
		for( int i=0; i<newCount; ++i ) {
			CreatureParam np = Cross(surviver);

			//-----------------------------------------------------
		　//　突然変異
			//-----------------------------------------------------
			if( i < param.mutationCount ) {
				np = Mutation(np);
      }

			//　交叉、突然変異した個体を次の世代に登録
		  param.creatureParams[i + param.surviveCount] = np;

		}
	}
}
