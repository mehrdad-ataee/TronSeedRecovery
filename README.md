A quick prototype I wrote for a quick test,
this is not a production ready tool by no mean and is
useful if you want to get some idea on how Tron
address generation algorithm works while using C#.

How to use:
1. write your seed phrase in the Resources/known_words.txt file in the following format:
word1,word2,word3,...,word12
if you can't remember the last 2 words then it should be like:
word1,word2,word3,...,word10
the file should only have 1 line and without any extra characters like // or #

2. write your wallet address in the Resources/target_address.txt file in the following format as an example:
TC8nfScKmy9Bs81jY4rs2ppWMGQrA4PjuD
the file should only have 1 line and without any extra characters like // or #

3. (optional) you can specify derivation path in the Resources/derivation_path.txt file.
you can read more about it here: https://learnmeabitcoin.com/technical/keys/hd-wallets/derivation-paths/

build or/and run WalletRecoveryTool.exe

combination table:
words ----- combinations
  1             256
  2           262,144
  3         536,870,912
  4       1,099,511,627,776
  don't even think about it
---------------------------
