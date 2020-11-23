# frozen_string_literal: true

task :conv => %i[conv:master]

namespace :conv do
  task :clean do
    rm_rf OUTPUT
  end

  desc 'マスターファイルの変換'
  task :master do
    sh 'ruby', 'Tools/XlsToPbConverter/xls2pb.rb', '-o', OUTPUT.to_s + "/master", (DATA_DIR + 'Master').to_s
  rescue StandardError
    puts "!!!!!!!!!!!! データファイルのコンバートエラー !!!!!!!!!!!!"
  end
end
