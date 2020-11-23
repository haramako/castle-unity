# frozen_string_literal: true

# 自動ビルド関係のRakefile

require 'json'

APP_NAME = 'castle'

# デプロイゲートにファイルをアップロードする
def upload_deploygate(path, message)
  require 'net/http'
  require 'openssl'

  # WindowsのRuby2.3でSSLのキーがおかしいので、ここで証明書を手動で設定する
  url = URI.parse("https://deploygate.com/api/users/#{DEPLOYGATE_USER}/apps")
  https = Net::HTTP.new(url.host, 443)
  https.use_ssl = true
  https.verify_mode = OpenSSL::SSL::VERIFY_NONE

  https.start do |s|
    req = Net::HTTP::Post.new(url.path)
    file = open(path, 'rb')
    req.set_form({ 'token' => DEPLOYGATE_TOKEN, 'file' => file, 'message' => message }, 'multipart/form-data')
    res = s.request(req)
    unless res.is_a? Net::HTTPSuccess
      puts res.body
      raise "アップロードに失敗しました! #{res}"
    end
  end
end

namespace :build do
  desc 'アプリをビルドする(PLATFORMに対象のプラットフォームを指定)'
  task :app do
    build_number = (ENV['BUILD_NUMBER'] || 1).to_i
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
    script_path = UNITY_PROJECT + 'Assets/Standard Assets/Scripts/AppBuilder/Editor/AppBuilder.cs'
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
       '-outputpath', 'AppBuilder') do |ok, _status|
      unless ok
        puts IO.read('Build.log')
        raise
      end
    end

    output = (IO.read(UNITY_PROJECT + 'AppBuilderOutput.txt') rescue raise "アプリのビルドに失敗しました(AppBuilderOutput.txtが見つかりません)")
    if output != 'OK'
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
      unless File.exist?("#{UNITY_PROJECT}/AppBuilder/Android/#{APP_NAME}.apk")
        puts IO.read('build.log')
        raise "ビルドに失敗しました"
      end
    else
      # tag = 'dfz-app-' + platform + '-' + commit_id[0, 8]
      # sh "#{CFSCTL} upload --tag #{tag} -o app.hash Client/AppBuilder/#{platform}"
      # upload_hash tag, 'app.hash', JSON.dump(desc)
    end
  end

  desc 'アプリのアップロード'
  task :upload_app do
    build_number = (ENV['BUILD_NUMBER'] || 0).to_i
    platform = get_target_platform(ENV['PLATFORM'] || 'windows')
    release = ENV['RELEASE'] # リリースビルドにする
    final = ENV['FINAL'] # ファイナルビルド（デバッグメニューOFF）にする
    enable_dmm = ENV['ENABLE_DMM']
    # use_streaming_assets = ENV['USE_STREAMING_ASSETS'] # Windowsのダウンロードなし版
    if final
      postfix = 'Final'
    elsif release
      postfix = 'Release'
    else
      postfix = 'Debug'
    end
    commit_id = `git show -s --format=%H`.strip[0, 8]
    branch = ENV['GIT_BRANCH'] || `git rev-parse --abbrev-ref HEAD`.strip
    branch = branch.gsub("origin/") { '' }.gsub(%r{/}) { '-' }

    case platform
    when "Android"
      upload_deploygate("#{UNITY_PROJECT}/AppBuilder/Android/#{APP_NAME}.apk", "#{postfix} build:#{build_number} branch:#{branch} commit:#{commit_id}")
    when "iOS"
      upload_deploygate("#{APP_NAME}.ipa", "#{postfix} build:#{build_number} branch:#{branch} commit:#{commit_id}")
    else
      raise "不正なプラットフォームです #{platform}"
    end
  end
end
