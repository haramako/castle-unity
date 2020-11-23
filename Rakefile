# frozen_string_literal: true

#
# Rakefile
#
$LOAD_PATH << './Tools/RubyLib'
$LOAD_PATH << './Tools/Converter'

require 'find'
require 'open-uri'
require 'open3'
require 'pp'
require 'logger'
require 'pathname'

require 'rake_common'
require 'path_detector'
# require 'deploy_mate'
# require 'conv_env'

detect_fullclean

# 実行ファイルのパス/拡張子
exe = ''
bin = 'Bin'
case RUBY_PLATFORM
when /darwin/
  # DO NOTHING
when /linux/
  bin = 'Bin/Linux'
else
  exe = '.exe'
end

# 実行ファイルのパス指定
ASTYLE = "./Tools/#{bin}/AStyle"

PROJECT_ROOT = Pathname.new('.')
UNITY_PROJECT = PROJECT_ROOT
TEMP = Pathname.new('Temp')
DATA_DIR = Pathname.new('Data')
OUTPUT = TEMP + 'DataOutput'

Dir.glob('Tools/Rakefiles/*.rake').each do |f|
  load f
end
