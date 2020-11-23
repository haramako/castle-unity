# coding: utf-8
# frozen_string_literal: true

# いろいろのもののPathを取得する

def detect_path(pattern)
  require 'rake'
  files = FileList[pattern]
  if files.empty?
    puts "Warning: program not found #{pattern}"
    nil
  else
    file = files.select { |x| File.exist?(x) }.sort.last
    if file
      # puts "Detected #{file}"
      file
    else
      puts "Warning: program not found #{pattern}"
      nil
    end
  end
end

if RUBY_PLATFORM =~ /darwin/
  UNITY = detect_path([
  	'/Applications/Unity/Hub/Editor/2018.4.14f1/Unity.app/Contents',
  	'/Applications/Unity2018.4.14f1/Unity.app/Contents'
  	])
  UNITY_EXE = UNITY && UNITY + '/MacOS/Unity'
  MCS = 'mcs'
  XBUILD = 'xbuild'
  MONO = ['mono', '--debug']
else
  if ENV['PLATFORM'] =~ /switch/i
    UNITY = detect_path([
                          'C:/Program Files/Unity2019.1.13f1',
                          'D:/Program Files/Unity2019.1.13f1',
                          'C:/Program Files/Unity/Hub/Editor/2019.1.13f1'
                        ])
  else
    UNITY = detect_path([
                          'C:/Program Files/Unity2018.4.14f1',
                          'D:/Program Files/Unity2018.4.14f1',
                          'C:/Program Files/Unity/Hub/Editor/2018.4.14f1',
                          'D:/Unity2018.4.14'
                        ])
  end
  UNITY_EXE = UNITY.to_s + '/Editor/Unity.exe'
  XBUILD = detect_path([
                         'C:/Program Files (x86)/Microsoft Visual Studio/2017/Community/MSBuild/15.0/Bin/MSBuild.exe', # Visual Studio 2017 Community in C
                         'D:/Program Files (x86)/Microsoft Visual Studio/2017/Community/MSBuild/15.0/Bin/MSBuild.exe', # Visual Studio 2017 Community in D
                         'C:/Program Files (x86)/Microsoft Visual Studio/2017/BuildTools/MSBuild/*/Bin/MSBuild.exe', # choco install -y microsoft-build-tools
                         'C:/Program Files (x86)/Microsoft Visual Studio/2017/Professional/MSBuild/15.0/Bin/MSBuild.exe', # Visual Studio 2017 Professional in C
                       ])
  MCS = XBUILD && File.realpath(XBUILD + '/../Roslyn/csc.exe')
  MONO = []
end
