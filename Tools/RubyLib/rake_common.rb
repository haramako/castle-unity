# Fullclean関係
def detect_fullclean
  # fullcleanを行うバージョンをファイルから取得する
  fullclean_version = (begin
    IO.read('.full_clean_version').to_i
  rescue
    0
  end)
    # クリーンしないといけないときは、クリーンする
    # すべてをクリーンさせたい場合は、.full_clean_version の値を一つ上げる
    current_version = (begin
        IO.read('.current_version').to_i
      rescue
        0
      end)
  
    if current_version < fullclean_version
      puts 'Info: データバージョンが上がったのですべてクリーンします'
      rm_rf ['Output', 'Temp', FileList['.*.bucket'], FileList['*.bucket']]
      IO.write('.current_version', fullclean_version)
    end
  end
  