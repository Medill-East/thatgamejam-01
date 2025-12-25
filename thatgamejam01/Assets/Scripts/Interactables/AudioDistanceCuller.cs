using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioDistanceCuller : MonoBehaviour
{
    public Transform player;      // 拖入你的 PlayerCapsule
    public float maxDistance = 3f; // 设为 3
    private AudioSource _audio;

    void Start()
    {
        _audio = GetComponent<AudioSource>();
        // 如果没拖玩家，自动找（防止报错）
        if (player == null) 
            player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > maxDistance)
        {
            // 距离远了：彻底静音
            if (_audio.isPlaying) _audio.Pause(); // 或者 _audio.volume = 0;
        }
        else
        {
            // 距离近了：恢复播放
            if (!_audio.isPlaying) _audio.UnPause();
            // 如果你用了 Linear Rolloff，这里不需要手动设音量，Unity 会自己算
        }
    }
}
