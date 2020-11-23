# frozen_string_literal: true

# githook用のrake

namespace :githook do
  desc 'gitのhookを設定する'
  task :setup do
    if RUBY_PLATFORM =~ /darwin/
      path = ENV['PATH']
      head = "#!/bin/sh\nexport LANG=ja_JP.utf-8\nexport PATH=#{path}\nbundle exec rake "
    else
      head = "#!/bin/sh\nbundle exec rake "
    end
    mkdir_p '.git/hooks'
    IO.write('.git/hooks/pre-commit', head + 'githook:precommit')
    chmod 0o777, '.git/hooks/pre-commit'
  end

  desc 'precommitフック'
  task :precommit do
    puts 'precommit hook by rake githook:precommit'
    against = `git rev-parse --verify HEAD`
    diff_str = `git diff-index --cached --name-only #{against}`
    # '"' で始まる文字列は、日本語を含むので無視する
    diff_str.force_encoding(Encoding::ASCII_8BIT)
    diff_str = diff_str.split(/\n/).reject { |s| s.include? '"' }.join("\n")
    diff_str.force_encoding(Encoding::UTF_8)
    files = diff_str.encode(Encoding::UTF_8).split(/\n/)
    puts files

    # .csファイルのインデントを修正する
    cs_files = filter_own(files.select { |f| f =~ /\.cs$/ })
    unless cs_files.empty?
      cs_files = cs_files.select { |f| File.exist?(f) }
      puts "reindent #{cs_files.join(' ')} by astyle"
      sh ASTYLE, '-Q', '--options=.astyle', *cs_files
      sh 'git', 'add', *cs_files
    end
  end

  desc 'git/config に spriteatlas の hash 値を 0 に固定する filter を追加'
  task :set_zeroclean do
    sh 'git', 'config', 'filter.zeroclean.clean', 'sed -e "s/Hash: .*/Hash: 00000000000000000000000000000000/"'
  end
end
