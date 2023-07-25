
<template>
  <h1>{{ msg }}</h1>
  <h2>store.u_info.name：{{ store.u_info.name }}</h2>
  <input type="text" v-model="user_info.name" @input="i_change">
  <div class="card">
    <button type="button" @click="changeStore">count is {{ count }}</button>
  </div>
</template>
<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { useUserStore } from '../store/index'

const store = useUserStore()
const msg = ref('hello world!')
const count = ref(0)
const user_info = reactive({ id: 0, name: '' })
const changeStore = () => {
  user_info.id = 30
  user_info.name = 'zhang san'
}
const i_change = () => {
  console.log('change',user_info.name)
  store.u_info = user_info
}
onMounted(() => {
  if (store.u_info) {
    msg.value = store.u_info.name
    console.log('you')
  } else {
    const n_use = {
      id: 1,
      name: 'taony'
    }
    Object.assign(user_info, n_use)
    store.u_info = user_info
  }
}) 
</script>


<style scoped>
.read-the-docs {
  color: #888;
}
</style>
