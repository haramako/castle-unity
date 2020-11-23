# coding: utf-8
# frozen_string_literal: true

# 自動ビルド関係のRakefile

require 'json'

# rubocop: disable Metrics/BlockLength
namespace :build do
  desc 'アプリをビルドする(PLATFORMに対象のプラットフォームを指定)'
  task :app do
    build_number = (ENV['BUILD_NUMBER'] || 0).to_i
    platform = get_target_platform(ENV['PLATFORM'] || 'windows')
    release = ENV['RELEASE'] # リリースビルドにする
    final = ENV['FINAL']
    if final
      postfix = 'Final'
    elsif release
      postfix = 'Release'
    else
      postfix = 'Debug'
    end
  
    puts "Platform = #{platform}"
  
    # MEMO: ファイルに何らかの変更を加えないと、コンパイルが正しく動かないのか AppBuilder.Build() が見つからないとエラーがでてしまうので、なんらかのタッチをする
    script_path = UNITY_PROJECT + 'Assets/Editor/AppBuilder/Editor/AppBuilder.cs'
    IO.binwrite(script_path, IO.binread(script_path) + " ")
  
    rm_f 'Client/AppBuilderOutput.txt' # 出力ファイルを削除する
    sh(UNITY_EXE, '-batchMode', '-quit', '-projectPath', UNITY_PROJECT.to_s,
       '-logFile', 'Build.log',
       '-executeMethod', 'AppBuilder.Build',
       '-target', platform,
       release ? '-release' : '',
       '-keyalias', 'aeVo6ien',
       '-buildVersion', BUILD_VERSION,
       '-buildNumber', build_number.to_s,
       '-outputpath', 'AppBuilder',
       ) do |ok, _status|
      unless ok
        puts IO.read('Build.log')
        raise
      end
    end
  
    output = (IO.read(UNITY_PROJ+'/AppBuilderOutput.txt') rescue raise "アプリのビルドに失敗しました(AppBuilderOutput.txtが見つかりません)")
    if output != ''
      puts "Error: #{output}"
      raise "アプリのビルドに失敗しました"
    end
  
    commit_id = `git show -s --format=%H`.strip
    branch = ENV['GIT_BRANCH'] || `git rev-parse --abbrev-ref HEAD`.strip
    git_log = `git log --date=iso -n 10 --pretty=format:"[%ad] %s"`
    desc = {
      build_number: build_number,
      branch: branch,
      date: Time.now.to_s,
      commit_id: commit_id,
      platform: platform,
      log: git_log
    }
  
    # アップロードする
    case platform
    when "Android"
      unless File.exist?('Client/AppBuilder/Android/dfz.apk')
        puts IO.read('build.log')
        raise "ビルドに失敗しました"
      end
    else
      tag = 'dfz-app-' + platform + '-' + commit_id[0, 8]
      sh "#{CFSCTL} upload --tag #{tag} -o app.hash Client/AppBuilder/#{platform}"
      upload_hash tag, 'app.hash', JSON.dump(desc)
    end
  end
end
